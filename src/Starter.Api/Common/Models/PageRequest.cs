namespace Starter.Api.Common;

public record PageRequest(
    int Page = PaginationDefaults.Page,
    int PageSize = PaginationDefaults.PageSize,
    string? SortBy = null,
    QueryEnum<SortDirection>? SortDirection = null);
