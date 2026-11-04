namespace Starter.Api.Common;

public record PageRequest
{
    public int Page { get; init; } = PaginationDefaults.Page;

    public int PageSize { get; init; } = PaginationDefaults.PageSize;

    public string? SortBy { get; init; }

    public SortDirection? SortDirection { get; init; }
}
