using FluentValidation;

using Starter.Api.Common;

namespace Starter.Api.AiUsage;

public sealed class AiUsageReportRequestValidator : AbstractValidator<AiUsageReportRequest>
{
    public AiUsageReportRequestValidator()
    {
        this.AddPageRules(AiUsageConstants.SortableFields);
        RuleFor(request => request.From).NotNull();
        RuleFor(request => request.To)
            .NotNull()
            .Must((request, to) => request.From is null || to > request.From)
            .WithMessage("O fim do período precisa ser depois do início.")
            .Must((request, to) => request.From is null || to - request.From <= TimeSpan.FromDays(AiUsageConstants.MaxReportDays))
            .WithMessage($"O período pode ter no máximo {AiUsageConstants.MaxReportDays} dias.");
    }
}
