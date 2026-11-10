using Starter.Api.Common;

namespace Starter.Api.AiUsage;

public sealed record AiUsageReportRequest(
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    int Page = PaginationDefaults.Page,
    int PageSize = PaginationDefaults.PageSize,
    string? SortBy = null,
    QueryEnum<SortDirection>? SortDirection = null) : PageRequest(Page, PageSize, SortBy, SortDirection);
