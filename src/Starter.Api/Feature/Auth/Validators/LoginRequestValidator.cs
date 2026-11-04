using FluentValidation;
using Starter.Api.Users;

namespace Starter.Api.Auth;

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(request => request.Email).NotEmpty().MaximumLength(UsersConstants.EmailMaxLength);
        RuleFor(request => request.Password).NotEmpty().MaximumLength(AuthConstants.PasswordMaxLength);
    }
}
