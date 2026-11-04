using System.Linq.Expressions;

using Starter.Api.Common;
using Starter.Api.Persistence;

namespace Starter.Api.Tests.Common;

public sealed class ApplySortTests
{
    private sealed record Row(int Id, string Name);

    private static readonly IReadOnlyDictionary<string, Expression<Func<Row, object>>> Sortable =
        new Dictionary<string, Expression<Func<Row, object>>> { ["name"] = row => row.Name };

    private static readonly IQueryable<Row> Rows = new[] { new Row(1, "b"), new Row(2, "a") }.AsQueryable();

    [Fact]
    public void Requested_field_missing_from_the_map_throws_naming_the_field()
    {
        var act = () => Rows.ApplySort(new PageRequest(SortBy: "createdAt"), Sortable, "name", SortDirection.Asc, row => row.Id);

        act.Should().Throw<ArgumentException>().WithMessage("*createdAt*");
    }

    [Fact]
    public void Default_field_missing_from_the_map_throws_naming_the_field()
    {
        var act = () => Rows.ApplySort(new PageRequest(), Sortable, "createdAt", SortDirection.Asc, row => row.Id);

        act.Should().Throw<ArgumentException>().WithMessage("*createdAt*");
    }

    [Fact]
    public void Known_field_sorts_ignoring_case()
    {
        var sorted = Rows.ApplySort(new PageRequest(SortBy: "NAME"), Sortable, "name", SortDirection.Asc, row => row.Id).ToList();

        sorted.Select(row => row.Id).Should().Equal(2, 1);
    }
}
