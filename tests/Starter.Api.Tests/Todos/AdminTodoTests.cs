using System.Net;
using Starter.Api.Tests.Infra;
using Starter.Api.Todos;

namespace Starter.Api.Tests.Todos;

[Collection(nameof(ApiCollection))]
public sealed class AdminTodoTests(ApiFactory factory)
{
    [Fact]
    public async Task Regular_user_gets_403()
    {
        var client = await TodoClient.ForNewUserAsync(factory);

        (await client.AdminListAsync()).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Administrator_sees_todos_of_everyone_with_owner_id()
    {
        var first = await Scenarios.RegisterAsync(factory);
        var second = await Scenarios.RegisterAsync(factory);
        var firstClient = TodoClient.For(factory.CreateClient(), first);
        var secondClient = TodoClient.For(factory.CreateClient(), second);
        var mine = await firstClient.CreateOkAsync("First owner");
        var theirs = await secondClient.CreateOkAsync("Second owner");
        var admin = await TodoClient.ForNewAdministratorAsync(factory);

        var page = await admin.AdminListOkAsync("pageSize=100");

        page.Items.Single(item => item.Id == mine.Id).OwnerId.Should().Be(first.UserId);
        page.Items.Single(item => item.Id == theirs.Id).OwnerId.Should().Be(second.UserId);
        page.Items.Single(item => item.Id == mine.Id).Title.Should().Be("First owner");
    }

    [Fact]
    public async Task Without_token_returns_401()
    {
        (await TodoClient.Anonymous(factory).AdminListAsync()).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Admin_listing_uses_the_same_pagination()
    {
        var owner = await TodoClient.ForNewUserAsync(factory);
        for (var index = 0; index < 3; index++)
        {
            await owner.CreateOkAsync($"Admin page {index}");
        }

        var admin = await TodoClient.ForNewAdministratorAsync(factory);

        var first = await admin.AdminListOkAsync("page=1&pageSize=2&sortBy=title&sortDirection=asc");
        var second = await admin.AdminListOkAsync("page=2&pageSize=2&sortBy=title&sortDirection=asc");
        var beyond = await admin.AdminListOkAsync("page=9999&pageSize=2");

        first.Page.Should().Be(1);
        first.PageSize.Should().Be(2);
        first.Items.Should().HaveCount(2);
        first.TotalCount.Should().BeGreaterThanOrEqualTo(3);
        first.TotalPages.Should().Be((first.TotalCount + 1) / 2);
        first.Items.Select(item => item.Id).Should().NotIntersectWith(second.Items.Select(item => item.Id));
        beyond.Items.Should().BeEmpty();
        (await admin.AdminListAsync("pageSize=0")).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await admin.AdminListAsync("sortBy=nope")).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await admin.AdminListAsync("status=foo")).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
