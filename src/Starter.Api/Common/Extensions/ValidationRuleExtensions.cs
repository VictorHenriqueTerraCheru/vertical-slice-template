using FluentValidation;

namespace Starter.Api.Common;

public static class ValidationRuleExtensions
{
    public static IRuleBuilderOptions<T, string> NoNullCharacter<T>(this IRuleBuilder<T, string> rule) =>
        rule.Must(value => value is null || !value.Contains('\0')).WithMessage("O campo contém caracteres inválidos.");
}
