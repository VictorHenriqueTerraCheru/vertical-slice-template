using System.Net;

using Starter.Api.Common;
using Starter.Api.Tests.Infra;
using Starter.Api.Users;

namespace Starter.Api.Tests.Users;

[Collection(nameof(ApiCollection))]
public sealed class MeTests(ApiFactory factory)
{
    [Fact]
    public async Task Me_returns_the_user_from_the_token()
    {
        var user = await Scenarios.RegisterAsync(factory);

        var response = await user.Client.GetMeAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var me = await ApiClient.ReadAsync<UserResponse>(response);
        me.Email.Should().Be(user.Email);
        me.Name.Should().Be("Test User");
        me.Role.Should().Be(UserRole.User);
    }

    [Fact]
    public async Task Me_without_token_returns_401()
    {
        var client = new ApiClient(factory.CreateClient());

        (await client.GetMeAsync()).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Updating_the_name_works()
    {
        var user = await Scenarios.RegisterAsync(factory);

        var update = await user.Client.UpdateMeAsync("New Name");

        update.StatusCode.Should().Be(HttpStatusCode.OK);
        var me = await ApiClient.ReadAsync<UserResponse>(await user.Client.GetMeAsync());
        me.Name.Should().Be("New Name");
    }

    [Fact]
    public async Task Parallel_updates_answer_only_200_or_409()
    {
        var user = await Scenarios.RegisterAsync(factory);

        var responses = await Task.WhenAll(Enumerable.Range(0, 8).Select(index => user.Client.UpdateMeAsync($"Name {index}")));

        responses.Select(response => response.StatusCode).Should()
            .OnlyContain(status => status == HttpStatusCode.OK || status == HttpStatusCode.Conflict);
        responses.Should().Contain(response => response.StatusCode == HttpStatusCode.OK);
    }

    [Fact]
    public async Task Updating_with_an_empty_name_returns_400()
    {
        var user = await Scenarios.RegisterAsync(factory);

        (await user.Client.UpdateMeAsync(" ")).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
