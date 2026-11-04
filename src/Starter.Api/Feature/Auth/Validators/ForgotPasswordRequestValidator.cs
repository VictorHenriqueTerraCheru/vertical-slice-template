using System.Text.Json;
using FluentValidation;
using Starter.Api.Common;
using Starter.Api.Users;

namespace Starter.Api.Auth;

public sealed class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequest>
{
    public ForgotPasswordRequestValidator()
    {
        RuleFor(request => (request.Email ?? string.Empty).Trim())
            .NotEmpty()
            .OverridePropertyName(JsonNamingPolicy.CamelCase.ConvertName(nameof(ForgotPasswordRequest.Email)))
            .EmailAddress()
            .MaximumLength(UsersConstants.EmailMaxLength)
            .NoNullCharacter();
    }
}
