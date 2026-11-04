using FluentValidation;
using Starter.Api.Common;

namespace Starter.Api.Auth;

public sealed class RefreshRequestValidator : AbstractValidator<RefreshRequest>
{
    public RefreshRequestValidator()
    {
        RuleFor(request => request.RefreshToken).NotEmpty().MaximumLength(AuthConstants.RefreshTokenMaxLength).NoNullCharacter();
    }
}
