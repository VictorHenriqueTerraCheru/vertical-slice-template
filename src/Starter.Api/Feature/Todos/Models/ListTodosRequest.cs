using Starter.Api.Common;

namespace Starter.Api.Todos;

public sealed record ListTodosRequest(
    QueryEnum<TodoStatus>? Status = null,
    int Page = PaginationDefaults.Page,
    int PageSize = PaginationDefaults.PageSize,
    string? SortBy = null,
    QueryEnum<SortDirection>? SortDirection = null) : PageRequest(Page, PageSize, SortBy, SortDirection);
