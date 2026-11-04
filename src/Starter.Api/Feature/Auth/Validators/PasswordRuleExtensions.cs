using FluentValidation;

namespace Starter.Api.Auth;

public static class PasswordRuleExtensions
{
    public static IRuleBuilderOptions<T, string> ValidNewPassword<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty()
            .MinimumLength(AuthConstants.PasswordMinLength)
            .WithMessage($"A senha deve ter pelo menos {AuthConstants.PasswordMinLength} caracteres.")
            .MaximumLength(AuthConstants.PasswordMaxLength)
            .WithMessage($"A senha deve ter no máximo {AuthConstants.PasswordMaxLength} caracteres.");
}
