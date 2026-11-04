using System.Text.Json;
using Starter.Api.Tests.Infra;

namespace Starter.Api.Tests.Todos;

[Collection(nameof(ApiCollection))]
public sealed class TodoOpenApiTests(ApiFactory factory)
{
    private async Task<IReadOnlyList<string>> EnumValuesAsync(string parameterName)
    {
        var document = await ApiClient.ReadJsonAsync(await factory.CreateClient().GetAsync("/openapi/v1.json"));
        var parameter = document.GetProperty("paths").GetProperty("/api/v1/todos").GetProperty("get").GetProperty("parameters")
            .EnumerateArray().Single(candidate => candidate.GetProperty("name").GetString() == parameterName);
        return parameter.GetProperty("schema").GetProperty("enum").EnumerateArray().Select(value => value.GetString()!).ToList();
    }

    [Fact]
    public async Task Status_parameter_lists_its_camelCase_values()
    {
        (await EnumValuesAsync("status")).Should().Equal("pending", "inProgress", "done", "canceled");
    }

    [Fact]
    public async Task SortDirection_parameter_lists_its_camelCase_values()
    {
        (await EnumValuesAsync("sortDirection")).Should().Equal("asc", "desc");
    }
}
