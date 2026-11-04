using FluentValidation;
using Starter.Api.Common;

namespace Starter.Api.Auth;

public sealed class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(request => request.CurrentPassword).NotEmpty().MaximumLength(AuthConstants.PasswordMaxLength);
        RuleFor(request => request.NewPassword).ValidNewPassword();
    }
}
