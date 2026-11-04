using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Starter.Api.Common;

namespace Starter.Api.Persistence;

public static class QueryablePagingExtensions
{
    public static IOrderedQueryable<T> ApplySort<T>(
        this IQueryable<T> query,
        PageRequest request,
        IReadOnlyDictionary<string, Expression<Func<T, object>>> sortable,
        string defaultField,
        SortDirection defaultDirection,
        Expression<Func<T, object>> tieBreaker)
    {
        var lookup = new Dictionary<string, Expression<Func<T, object>>>(sortable, StringComparer.OrdinalIgnoreCase);
        var key = request.SortBy is not null && lookup.ContainsKey(request.SortBy) ? request.SortBy : defaultField;
        var direction = request.SortDirection ?? defaultDirection;
        var ordered = direction == SortDirection.Desc ? query.OrderByDescending(lookup[key]) : query.OrderBy(lookup[key]);
        return ordered.ThenBy(tieBreaker);
    }

    public static async Task<PagedResponse<TItem>> ToPagedAsync<TEntity, TItem>(
        this IOrderedQueryable<TEntity> query,
        PageRequest request,
        Expression<Func<TEntity, TItem>> selector,
        CancellationToken cancellationToken)
    {
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(selector)
            .ToListAsync(cancellationToken);
        return PagedResponse<TItem>.Create(items, request.Page, request.PageSize, totalCount);
    }
}
