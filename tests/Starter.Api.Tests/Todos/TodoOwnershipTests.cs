using System.Net;
using System.Text.RegularExpressions;

using Starter.Api.Tests.Infra;
using Starter.Api.Todos;

namespace Starter.Api.Tests.Todos;

[Collection(nameof(ApiCollection))]
public sealed class TodoOwnershipTests(ApiFactory factory)
{
    [Fact]
    public async Task Todo_of_another_user_answers_404_on_get_put_and_delete_like_a_missing_id()
    {
        var owner = await TodoClient.ForNewUserAsync(factory);
        var intruder = await TodoClient.ForNewUserAsync(factory);
        var todo = await owner.CreateOkAsync("Private");
        var missing = Guid.NewGuid();

        var foreign = new[]
        {
            await intruder.GetAsync(todo.Id),
            await intruder.UpdateAsync(todo.Id, "Hijacked"),
            await intruder.DeleteAsync(todo.Id),
        };
        var absent = new[]
        {
            await intruder.GetAsync(missing),
            await intruder.UpdateAsync(missing, "Hijacked"),
            await intruder.DeleteAsync(missing),
        };

        foreign.Should().OnlyContain(response => response.StatusCode == HttpStatusCode.NotFound);
        for (var index = 0; index < foreign.Length; index++)
        {
            foreign[index].Content.Headers.ContentType?.ToString().Should().Be(absent[index].Content.Headers.ContentType?.ToString());
            (await WithoutTraceIdAsync(foreign[index])).Should().Be(await WithoutTraceIdAsync(absent[index]));
        }

        var untouched = await TodoClient.ReadAsync<TodoItemResponse>(await owner.GetAsync(todo.Id));
        untouched.Title.Should().Be("Private");
    }

    private static async Task<string> WithoutTraceIdAsync(HttpResponseMessage response) =>
        Regex.Replace(await response.Content.ReadAsStringAsync(), "\"traceId\":\"[^\"]*\"", string.Empty);

    [Fact]
    public async Task Listing_only_returns_the_todos_of_the_user()
    {
        var first = await TodoClient.ForNewUserAsync(factory);
        var second = await TodoClient.ForNewUserAsync(factory);
        var mine = await first.CreateOkAsync("Mine");
        await second.CreateOkAsync("Theirs");

        var page = await first.ListOkAsync();

        page.TotalCount.Should().Be(1);
        page.Items.Select(item => item.Id).Should().Equal(mine.Id);
    }

    [Fact]
    public async Task Without_token_every_route_returns_401()
    {
        var owner = await TodoClient.ForNewUserAsync(factory);
        var todo = await owner.CreateOkAsync("Private");
        var anonymous = TodoClient.Anonymous(factory);

        var responses = new[]
        {
            await anonymous.CreateAsync("Title"),
            await anonymous.ListAsync(),
            await anonymous.GetAsync(todo.Id),
            await anonymous.UpdateAsync(todo.Id, "Title"),
            await anonymous.DeleteAsync(todo.Id),
        };

        responses.Should().OnlyContain(response => response.StatusCode == HttpStatusCode.Unauthorized);
    }
}
