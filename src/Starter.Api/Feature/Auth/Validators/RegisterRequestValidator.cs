using FluentValidation;
using Starter.Api.Users;

namespace Starter.Api.Auth;

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(request => request.Name).NotEmpty().MaximumLength(UsersConstants.NameMaxLength);
        RuleFor(request => request.Email).NotEmpty().EmailAddress().MaximumLength(UsersConstants.EmailMaxLength);
        RuleFor(request => request.Password)
            .NotEmpty()
            .MinimumLength(AuthConstants.PasswordMinLength)
            .WithMessage($"A senha deve ter pelo menos {AuthConstants.PasswordMinLength} caracteres.")
            .MaximumLength(AuthConstants.PasswordMaxLength)
            .WithMessage($"A senha deve ter no máximo {AuthConstants.PasswordMaxLength} caracteres.");
    }
}
