using System.Net;
using System.Net.Http.Json;

using Starter.Api.AiUsage;
using Starter.Api.Persistence;
using Starter.Api.Tests.Infra;

namespace Starter.Api.Tests.AiUsage;

[Collection(nameof(ApiCollection))]
public sealed class AiUsageReportTests(ApiFactory factory)
{
    private const string Period = "from=2001-03-01T00:00:00Z&to=2001-04-01T00:00:00Z";

    private static AiUsageEntry Entry(string model, AiCallStatus status, int input, DateTimeOffset at) => new()
    {
        Id = Guid.CreateVersion7(at),
        Feature = "test",
        Provider = "openai",
        Model = model,
        Status = status,
        InputTokens = input,
        CachedInputTokens = input / 2,
        OutputTokens = 5,
        LatencyMilliseconds = 10,
        OccurredAt = at,
    };

    [Fact]
    public async Task Administrator_sees_usage_grouped_by_model_inside_the_period()
    {
        var first = $"model-a-{Guid.NewGuid():N}";
        var second = $"model-b-{Guid.NewGuid():N}";
        var inside = new DateTimeOffset(2001, 3, 10, 12, 0, 0, TimeSpan.Zero);
        await factory.InDatabaseAsync(db =>
        {
            db.Set<AiUsageEntry>().AddRange(
                Entry(first, AiCallStatus.Completed, 100, inside),
                Entry(first, AiCallStatus.Failed, 10, inside.AddHours(1)),
                Entry(second, AiCallStatus.Truncated, 40, inside),
                Entry(first, AiCallStatus.Completed, 999, new DateTimeOffset(2001, 4, 1, 0, 0, 0, TimeSpan.Zero)));
            return db.SaveChangesAsync();
        });
        var admin = await Scenarios.AdministratorClientAsync(factory);

        var response = await admin.GetAsync($"/api/v1/admin/ai-usage?{Period}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var report = await ApiClient.ReadAsync<AiUsageReport>(response);
        report.Models.Single(row => row.Model == first)
            .Should().Be(new AiUsageByModel("openai", first, 2, 1, 110, 55, 10));
        report.Models.Single(row => row.Model == second)
            .Should().Be(new AiUsageByModel("openai", second, 1, 0, 40, 20, 5));
    }

    [Fact]
    public async Task Regular_user_gets_403()
    {
        var user = await Scenarios.RegisterAsync(factory);

        var response = await Scenarios.ClientFor(factory, user).GetAsync($"/api/v1/admin/ai-usage?{Period}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData("")]
    [InlineData("from=2001-03-01T00:00:00Z")]
    [InlineData("from=2001-04-01T00:00:00Z&to=2001-03-01T00:00:00Z")]
    [InlineData("from=2001-01-01T00:00:00Z&to=2002-02-01T00:00:00Z")]
    public async Task Invalid_period_returns_400(string query)
    {
        var admin = await Scenarios.AdministratorClientAsync(factory);

        var response = await admin.GetAsync($"/api/v1/admin/ai-usage?{query}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>()).GetProperty("errors").EnumerateObject().Should().NotBeEmpty();
    }
}
