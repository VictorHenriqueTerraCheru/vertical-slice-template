using FluentValidation;

namespace Starter.Api.Common;

public static class PageRequestValidationExtensions
{
    public static void AddPageRules<T>(this AbstractValidator<T> validator, IReadOnlyCollection<string> sortableFields)
        where T : PageRequest
    {
        validator.RuleFor(request => request.Page)
            .InclusiveBetween(1, PaginationDefaults.MaxPage)
            .WithMessage($"A página deve estar entre 1 e {PaginationDefaults.MaxPage}.");

        validator.RuleFor(request => request.PageSize)
            .InclusiveBetween(1, PaginationDefaults.MaxPageSize)
            .WithMessage($"O tamanho da página deve estar entre 1 e {PaginationDefaults.MaxPageSize}.");

        validator.RuleFor(request => request.SortBy)
            .Must(sortBy => sortableFields.Contains(sortBy!, StringComparer.OrdinalIgnoreCase))
            .When(request => request.SortBy is not null)
            .WithMessage($"A ordenação deve ser um dos campos: {string.Join(", ", sortableFields)}.");
    }
}
