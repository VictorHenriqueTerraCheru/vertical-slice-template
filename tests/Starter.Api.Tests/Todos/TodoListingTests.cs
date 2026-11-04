using System.Net;

using Starter.Api.Tests.Infra;
using Starter.Api.Todos;

namespace Starter.Api.Tests.Todos;

[Collection(nameof(ApiCollection))]
public sealed class TodoListingTests(ApiFactory factory)
{
    private static async Task<List<TodoItemResponse>> CreateManyAsync(TodoClient client, int count)
    {
        var created = new List<TodoItemResponse>();
        for (var index = 0; index < count; index++)
        {
            created.Add(await client.CreateOkAsync($"Todo {index:00}"));
        }

        return created;
    }

    [Fact]
    public async Task Defaults_are_page_1_with_20_items()
    {
        var client = await TodoClient.ForNewUserAsync(factory);
        await CreateManyAsync(client, 21);

        var page = await client.ListOkAsync();

        page.Page.Should().Be(1);
        page.PageSize.Should().Be(20);
        page.Items.Should().HaveCount(20);
        page.TotalCount.Should().Be(21);
        page.TotalPages.Should().Be(2);
    }

    [Fact]
    public async Task Response_has_items_page_pageSize_totalCount_and_totalPages()
    {
        var client = await TodoClient.ForNewUserAsync(factory);
        await CreateManyAsync(client, 3);

        var json = await ApiClient.ReadJsonAsync(await client.ListAsync("pageSize=2"));

        json.EnumerateObject().Select(property => property.Name).Should()
            .BeEquivalentTo("items", "page", "pageSize", "totalCount", "totalPages");
        json.GetProperty("items").GetArrayLength().Should().Be(2);
        json.GetProperty("totalCount").GetInt32().Should().Be(3);
        json.GetProperty("totalPages").GetInt32().Should().Be(2);
        json.GetProperty("items")[0].EnumerateObject().Select(property => property.Name).Should()
            .BeEquivalentTo("id", "title", "description", "status", "createdAt", "updatedAt");
    }

    [Fact]
    public async Task Pages_neither_repeat_nor_lose_items_with_equal_createdAt()
    {
        var client = await TodoClient.ForNewUserAsync(factory);
        var created = await CreateManyAsync(client, 5);
        created.Select(item => item.CreatedAt).Distinct().Should().ContainSingle();

        var seen = new List<Guid>();
        foreach (var page in new[] { 1, 2, 3 })
        {
            var result = await client.ListOkAsync($"page={page}&pageSize=2");
            result.TotalCount.Should().Be(5);
            result.TotalPages.Should().Be(3);
            seen.AddRange(result.Items.Select(item => item.Id));
        }

        seen.Should().HaveCount(5);
        seen.Should().OnlyHaveUniqueItems();
        seen.Should().BeEquivalentTo(created.Select(item => item.Id));
    }

    [Fact]
    public async Task Page_beyond_the_end_returns_200_empty_with_correct_totals()
    {
        var client = await TodoClient.ForNewUserAsync(factory);
        await CreateManyAsync(client, 3);

        var page = await client.ListOkAsync("page=9&pageSize=2");

        page.Items.Should().BeEmpty();
        page.Page.Should().Be(9);
        page.TotalCount.Should().Be(3);
        page.TotalPages.Should().Be(2);
    }

    [Fact]
    public async Task Empty_listing_returns_200_with_zero_totals()
    {
        var client = await TodoClient.ForNewUserAsync(factory);

        var page = await client.ListOkAsync();

        page.Items.Should().BeEmpty();
        page.TotalCount.Should().Be(0);
        page.TotalPages.Should().Be(0);
    }

    [Theory]
    [InlineData("pageSize=101")]
    [InlineData("pageSize=0")]
    [InlineData("page=0")]
    [InlineData("page=10001")]
    public async Task Page_values_out_of_range_return_400(string query)
    {
        var client = await TodoClient.ForNewUserAsync(factory);

        var response = await client.ListAsync(query);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ApiClient.ReadJsonAsync(response)).GetProperty("errors").EnumerateObject().Should().NotBeEmpty();
    }

    [Fact]
    public async Task Unknown_sortBy_returns_400()
    {
        var client = await TodoClient.ForNewUserAsync(factory);

        var response = await client.ListAsync("sortBy=password");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ApiClient.ReadJsonAsync(response)).GetProperty("errors").TryGetProperty("sortBy", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Sorts_by_title_asc_and_desc()
    {
        var client = await TodoClient.ForNewUserAsync(factory);
        foreach (var title in new[] { "banana", "cherry", "apple" })
        {
            await client.CreateOkAsync(title);
        }

        var ascending = await client.ListOkAsync("sortBy=title&sortDirection=asc");
        var descending = await client.ListOkAsync("sortBy=title&sortDirection=desc");
        var defaultDirection = await client.ListOkAsync("sortBy=title");

        ascending.Items.Select(item => item.Title).Should().Equal("apple", "banana", "cherry");
        descending.Items.Select(item => item.Title).Should().Equal("cherry", "banana", "apple");
        defaultDirection.Items.Select(item => item.Title).Should().Equal("cherry", "banana", "apple");
    }

    [Fact]
    public async Task Sorts_by_createdAt_asc_when_requested()
    {
        var client = await TodoClient.ForNewUserAsync(factory);
        var first = await client.CreateOkAsync("First");
        factory.Clock.Advance(TimeSpan.FromSeconds(1));
        var second = await client.CreateOkAsync("Second");

        var page = await client.ListOkAsync("sortBy=createdAt&sortDirection=asc");

        page.Items.Select(item => item.Id).Should().Equal(first.Id, second.Id);
    }

    [Fact]
    public async Task Default_order_is_most_recent_first()
    {
        var client = await TodoClient.ForNewUserAsync(factory);
        var first = await client.CreateOkAsync("First");
        factory.Clock.Advance(TimeSpan.FromSeconds(1));
        var second = await client.CreateOkAsync("Second");
        factory.Clock.Advance(TimeSpan.FromSeconds(1));
        var third = await client.CreateOkAsync("Third");

        var page = await client.ListOkAsync();

        page.Items.Select(item => item.Id).Should().Equal(third.Id, second.Id, first.Id);
    }

    [Fact]
    public async Task Filters_by_status()
    {
        var client = await TodoClient.ForNewUserAsync(factory);
        var pending = await client.CreateOkAsync("Pending");
        var done = await client.CreateOkAsync("Done");
        await TodoDatabase.SetStatusAsync(factory, done.Id, TodoStatus.Done);

        var donePage = await client.ListOkAsync("status=done");
        var pendingPage = await client.ListOkAsync("status=Pending");
        var canceledPage = await client.ListOkAsync("status=canceled");
        var all = await client.ListOkAsync();

        donePage.Items.Select(item => item.Id).Should().Equal(done.Id);
        donePage.TotalCount.Should().Be(1);
        pendingPage.Items.Select(item => item.Id).Should().Equal(pending.Id);
        canceledPage.Items.Should().BeEmpty();
        all.TotalCount.Should().Be(2);
    }

    [Theory]
    [InlineData("page=abc")]
    [InlineData("status=foo")]
    [InlineData("status=99")]
    [InlineData("sortDirection=x")]
    [InlineData("pageSize=")]
    public async Task Malformed_query_returns_400_in_problem_details(string query)
    {
        var client = await TodoClient.ForNewUserAsync(factory);

        var response = await client.ListAsync(query);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task Id_that_is_not_a_guid_in_the_route_returns_404()
    {
        var client = await TodoClient.ForNewUserAsync(factory);

        var response = await client.GetPathAsync("/api/v1/todos/not-a-guid");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }
}
