using System.Net;

using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Starter.Api.Persistence;
using Starter.Api.Tests.Infra;
using Starter.Api.Todos;

namespace Starter.Api.Tests.Todos;

[Collection(nameof(ApiCollection))]
public sealed class TodoTransitionFlowTests(ApiFactory factory)
{
    private Task<int> CountEventsAsync(Guid todoId) =>
        factory.InDatabaseAsync(db => db.Set<TodoEvent>().CountAsync(row => row.TodoItemId == todoId));

    [Fact]
    public async Task Valid_transition_changes_status_and_records_the_event()
    {
        var user = await Scenarios.RegisterAsync(factory);
        var client = TodoClient.For(factory.CreateClient(), user);
        var todo = await client.CreateOkAsync("Flow");
        factory.Clock.Advance(TimeSpan.FromMinutes(1));

        var response = await client.TransitionAsync(todo.Id, TodoStatus.InProgress);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var moved = await TodoClient.ReadAsync<TodoItemResponse>(response);
        moved.Status.Should().Be(TodoStatus.InProgress);
        moved.UpdatedAt.Should().Be(factory.Clock.GetUtcNow());
        var events = await client.EventsOkAsync(todo.Id);
        var recorded = events.Should().ContainSingle().Subject;
        recorded.FromStatus.Should().Be(TodoStatus.Pending);
        recorded.ToStatus.Should().Be(TodoStatus.InProgress);
        recorded.ActorId.Should().Be(user.UserId);
        recorded.OccurredAt.Should().Be(factory.Clock.GetUtcNow());
    }

    [Fact]
    public async Task Invalid_transition_returns_409_without_event()
    {
        var client = await TodoClient.ForNewUserAsync(factory);
        var todo = await client.CreateOkAsync("Flow");

        var response = await client.TransitionAsync(todo.Id, TodoStatus.Done);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await TodoClient.ReadProblemAsync(response)).GetProperty("type").GetString().Should().Be("invalid-transition");
        (await CountEventsAsync(todo.Id)).Should().Be(0);
        (await TodoDatabase.FindAsync(factory, todo.Id))!.Status.Should().Be(TodoStatus.Pending);
    }

    [Fact]
    public async Task Same_status_transition_returns_409()
    {
        var client = await TodoClient.ForNewUserAsync(factory);
        var todo = await client.CreateOkAsync("Flow");

        (await client.TransitionAsync(todo.Id, TodoStatus.Pending)).StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Theory]
    [InlineData("{\"status\":\"inprogress\"}", HttpStatusCode.OK)]
    [InlineData("{\"status\":\"InProgress\"}", HttpStatusCode.OK)]
    [InlineData("{\"status\":\"foo\"}", HttpStatusCode.BadRequest)]
    [InlineData("{\"status\":1}", HttpStatusCode.BadRequest)]
    [InlineData("{}", HttpStatusCode.BadRequest)]
    [InlineData("{broken", HttpStatusCode.BadRequest)]
    public async Task Transition_body_uses_case_insensitive_enum_names_and_rejects_the_rest(string body, HttpStatusCode expected)
    {
        var client = await TodoClient.ForNewUserAsync(factory);
        var todo = await client.CreateOkAsync("Flow");

        var response = await client.TransitionRawAsync(todo.Id, body);

        response.StatusCode.Should().Be(expected);
        if (expected == HttpStatusCode.BadRequest)
        {
            response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
            (await TodoDatabase.FindAsync(factory, todo.Id))!.Status.Should().Be(TodoStatus.Pending);
        }
    }

    [Fact]
    public async Task Failure_while_recording_the_event_rolls_back_the_status_change()
    {
        var user = await Scenarios.RegisterAsync(factory);
        var client = TodoClient.For(factory.CreateClient(), user);
        var todo = await client.CreateOkAsync("Flow");
        using var failing = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.AddScoped<ITodoEventRepository, FailingTodoEventRepository>()));
        var failingClient = TodoClient.For(failing.CreateClient(), user);

        var response = await failingClient.TransitionAsync(todo.Id, TodoStatus.InProgress);

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        (await TodoDatabase.FindAsync(factory, todo.Id))!.Status.Should().Be(TodoStatus.Pending);
        (await CountEventsAsync(todo.Id)).Should().Be(0);
    }

    [Fact]
    public async Task Same_transition_in_parallel_lets_only_one_win()
    {
        var client = await TodoClient.ForNewUserAsync(factory);
        var todo = await client.CreateOkAsync("Race");

        var responses = await Task.WhenAll(
            client.TransitionAsync(todo.Id, TodoStatus.InProgress),
            client.TransitionAsync(todo.Id, TodoStatus.InProgress));

        responses.Count(response => response.StatusCode == HttpStatusCode.OK).Should().Be(1);
        responses.Count(response => response.StatusCode == HttpStatusCode.Conflict).Should().Be(1);
        (await CountEventsAsync(todo.Id)).Should().Be(1);
        (await TodoDatabase.FindAsync(factory, todo.Id))!.Status.Should().Be(TodoStatus.InProgress);
    }

    [Fact]
    public async Task Events_come_in_order_of_occurrence()
    {
        var client = await TodoClient.ForNewUserAsync(factory);
        var todo = await client.CreateOkAsync("Order");
        await client.TransitionAsync(todo.Id, TodoStatus.InProgress);
        factory.Clock.Advance(TimeSpan.FromMinutes(1));
        await client.TransitionAsync(todo.Id, TodoStatus.Pending);
        factory.Clock.Advance(TimeSpan.FromMinutes(1));
        await client.TransitionAsync(todo.Id, TodoStatus.InProgress);
        factory.Clock.Advance(TimeSpan.FromMinutes(1));
        await client.TransitionAsync(todo.Id, TodoStatus.Done);

        var events = await client.EventsOkAsync(todo.Id);

        events.Select(row => (row.FromStatus, row.ToStatus)).Should().Equal(
            (TodoStatus.Pending, TodoStatus.InProgress),
            (TodoStatus.InProgress, TodoStatus.Pending),
            (TodoStatus.Pending, TodoStatus.InProgress),
            (TodoStatus.InProgress, TodoStatus.Done));
        events.Select(row => row.OccurredAt).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task Events_and_transitions_of_another_users_todo_answer_404()
    {
        var owner = await TodoClient.ForNewUserAsync(factory);
        var intruder = await TodoClient.ForNewUserAsync(factory);
        var todo = await owner.CreateOkAsync("Private");

        (await intruder.EventsAsync(todo.Id)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await intruder.TransitionAsync(todo.Id, TodoStatus.InProgress)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await intruder.EventsAsync(Guid.NewGuid())).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await TodoDatabase.FindAsync(factory, todo.Id))!.Status.Should().Be(TodoStatus.Pending);
    }

    [Theory]
    [InlineData(TodoStatus.Done)]
    [InlineData(TodoStatus.Canceled)]
    public async Task Put_on_a_finished_todo_returns_409_but_delete_is_still_allowed(TodoStatus finalStatus)
    {
        var client = await TodoClient.ForNewUserAsync(factory);
        var todo = await client.CreateOkAsync("Finished");
        await client.TransitionAsync(todo.Id, TodoStatus.InProgress);
        await TodoDatabase.SetStatusAsync(factory, todo.Id, finalStatus);

        var update = await client.UpdateAsync(todo.Id, "Changed");

        update.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await TodoDatabase.FindAsync(factory, todo.Id))!.Title.Should().Be("Finished");
        (await client.DeleteAsync(todo.Id)).StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Put_on_an_in_progress_todo_still_works()
    {
        var client = await TodoClient.ForNewUserAsync(factory);
        var todo = await client.CreateOkAsync("Open");
        await client.TransitionAsync(todo.Id, TodoStatus.InProgress);

        (await client.UpdateAsync(todo.Id, "Changed")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private sealed class FailingTodoEventRepository(AppDbContext db) : ITodoEventRepository
    {
        public void Add(TodoEvent row)
        {
            db.SaveChanges();
            throw new InvalidOperationException("event storage unavailable");
        }

        public Task<TodoEventResponse[]> ListAsync(Guid todoId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
