using FluentValidation;
using Starter.Api.Common;
using Starter.Api.Users;

namespace Starter.Api.Auth;

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(request => request.Email).NotEmpty().MaximumLength(UsersConstants.EmailMaxLength).NoNullCharacter();
        RuleFor(request => request.Password).NotEmpty().MaximumLength(AuthConstants.PasswordMaxLength);
    }
}
