using FluentValidation;
using Starter.Api.Common;
using Starter.Api.Users;

namespace Starter.Api.Auth;

public sealed class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(request => request.Email).NotEmpty().EmailAddress().MaximumLength(UsersConstants.EmailMaxLength).NoNullCharacter();
        RuleFor(request => request.Token).NotEmpty().MaximumLength(AuthConstants.PasswordResetTokenMaxLength).NoNullCharacter();
        RuleFor(request => request.NewPassword).ValidNewPassword();
    }
}
