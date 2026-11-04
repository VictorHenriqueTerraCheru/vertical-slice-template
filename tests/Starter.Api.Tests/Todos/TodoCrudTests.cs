using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Starter.Api.Common;
using Starter.Api.Persistence;
using Starter.Api.Tests.Infra;
using Starter.Api.Todos;

namespace Starter.Api.Tests.Todos;

[Collection(nameof(ApiCollection))]
public sealed class TodoCrudTests(ApiFactory factory)
{
    [Fact]
    public async Task Create_returns_201_with_pending_status()
    {
        var client = await TodoClient.ForNewUserAsync(factory);

        var response = await client.CreateAsync("Buy milk", "Two liters");

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var todo = await TodoClient.ReadAsync<TodoItemResponse>(response);
        response.Headers.Location!.ToString().Should().EndWith($"/api/v1/todos/{todo.Id}");
        todo.Title.Should().Be("Buy milk");
        todo.Description.Should().Be("Two liters");
        todo.Status.Should().Be(TodoStatus.Pending);
        todo.CreatedAt.Should().Be(factory.Clock.GetUtcNow());
        todo.UpdatedAt.Should().Be(todo.CreatedAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Empty_or_whitespace_title_returns_400(string title)
    {
        var client = await TodoClient.ForNewUserAsync(factory);

        var response = await client.CreateAsync(title);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ApiClient.ReadJsonAsync(response)).GetProperty("errors").TryGetProperty("title", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Missing_title_returns_400()
    {
        var client = await TodoClient.ForNewUserAsync(factory);

        (await client.PostRawAsync("{}")).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Title_longer_than_200_returns_400_and_exactly_200_is_accepted()
    {
        var client = await TodoClient.ForNewUserAsync(factory);

        (await client.CreateAsync(new string('a', 201))).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await client.CreateAsync(new string('a', 200))).StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Description_longer_than_2000_returns_400()
    {
        var client = await TodoClient.ForNewUserAsync(factory);

        (await client.CreateAsync("Valid", new string('a', 2001))).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Null_character_in_text_returns_400()
    {
        var client = await TodoClient.ForNewUserAsync(factory);

        (await client.CreateAsync("bad\0title")).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await client.CreateAsync("Valid", "bad\0description")).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Title_is_stored_without_surrounding_whitespace()
    {
        var client = await TodoClient.ForNewUserAsync(factory);

        var todo = await client.CreateOkAsync("  Buy milk  ", "  Two liters  ");

        todo.Title.Should().Be("Buy milk");
        var stored = await TodoDatabase.FindAsync(factory, todo.Id);
        stored!.Title.Should().Be("Buy milk");
        stored.Description.Should().Be("Two liters");
    }

    [Fact]
    public async Task Get_returns_the_todo()
    {
        var client = await TodoClient.ForNewUserAsync(factory);
        var created = await client.CreateOkAsync("Read");

        var response = await client.GetAsync(created.Id);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await TodoClient.ReadAsync<TodoItemResponse>(response)).Should().Be(created);
    }

    [Fact]
    public async Task Update_and_delete_work()
    {
        var client = await TodoClient.ForNewUserAsync(factory);
        var created = await client.CreateOkAsync("Old title", "Old description");
        factory.Clock.Advance(TimeSpan.FromMinutes(5));

        var update = await client.UpdateAsync(created.Id, "  New title ", null);

        update.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await TodoClient.ReadAsync<TodoItemResponse>(update);
        updated.Title.Should().Be("New title");
        updated.Description.Should().BeNull();
        updated.Status.Should().Be(TodoStatus.Pending);
        updated.CreatedAt.Should().Be(created.CreatedAt);
        updated.UpdatedAt.Should().Be(factory.Clock.GetUtcNow());

        (await client.DeleteAsync(created.Id)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await client.GetAsync(created.Id)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await client.DeleteAsync(created.Id)).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_with_invalid_title_returns_400_and_changes_nothing()
    {
        var client = await TodoClient.ForNewUserAsync(factory);
        var created = await client.CreateOkAsync("Keep");

        (await client.UpdateAsync(created.Id, "  ")).StatusCode.Should().Be(HttpStatusCode.BadRequest);

        (await TodoClient.ReadAsync<TodoItemResponse>(await client.GetAsync(created.Id))).Title.Should().Be("Keep");
    }

    [Fact]
    public async Task Update_of_a_missing_todo_returns_404()
    {
        var client = await TodoClient.ForNewUserAsync(factory);

        (await client.UpdateAsync(Guid.NewGuid(), "Title")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Malformed_json_returns_400()
    {
        var client = await TodoClient.ForNewUserAsync(factory);

        var response = await client.PostRawAsync("{\"title\": ");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task Stale_write_after_a_change_through_the_api_is_rejected_with_409()
    {
        var client = await TodoClient.ForNewUserAsync(factory);
        var created = await client.CreateOkAsync("Original");
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var stale = await db.Set<TodoItem>().FirstAsync(item => item.Id == created.Id);

        (await client.UpdateAsync(created.Id, "Changed through the API")).StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await unitOfWork.ExecuteInTransactionAsync<Empty>(_ =>
        {
            stale.Title = "Stale write";
            return Task.FromResult<Result<Empty>>(new Empty());
        }, CancellationToken.None);

        result.Error.Should().Be(CommonErrors.ConcurrentChange);
        (await TodoDatabase.FindAsync(factory, created.Id))!.Title.Should().Be("Changed through the API");
    }

    [Fact]
    public async Task Whitespace_only_description_is_stored_as_null()
    {
        var client = await TodoClient.ForNewUserAsync(factory);

        var created = await client.CreateOkAsync("Title", "   ");
        var updated = await TodoClient.ReadAsync<TodoItemResponse>(await client.UpdateAsync(created.Id, "Title", " \t "));

        created.Description.Should().BeNull();
        updated.Description.Should().BeNull();
        (await TodoDatabase.FindAsync(factory, created.Id))!.Description.Should().BeNull();
    }

    [Fact]
    public async Task Parallel_updates_answer_only_200_or_409()
    {
        var client = await TodoClient.ForNewUserAsync(factory);
        var created = await client.CreateOkAsync("Contended");

        var responses = await Task.WhenAll(Enumerable.Range(0, 16).Select(index => client.UpdateAsync(created.Id, $"Title {index}")));

        responses.Select(response => response.StatusCode).Should()
            .OnlyContain(status => status == HttpStatusCode.OK || status == HttpStatusCode.Conflict);
        responses.Should().Contain(response => response.StatusCode == HttpStatusCode.OK);
    }

    [Fact]
    public async Task Parallel_deletes_answer_only_204_404_or_409()
    {
        var client = await TodoClient.ForNewUserAsync(factory);
        var created = await client.CreateOkAsync("Doomed");

        var responses = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => client.DeleteAsync(created.Id)));

        responses.Select(response => response.StatusCode).Should()
            .OnlyContain(status => status == HttpStatusCode.NoContent || status == HttpStatusCode.NotFound || status == HttpStatusCode.Conflict);
        responses.Count(response => response.StatusCode == HttpStatusCode.NoContent).Should().Be(1);
        (await client.GetAsync(created.Id)).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
