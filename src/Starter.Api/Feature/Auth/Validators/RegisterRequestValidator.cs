using System.Text.Json;

using FluentValidation;

using Starter.Api.Common;
using Starter.Api.Users;

namespace Starter.Api.Auth;

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(request => request.Name).NotEmpty().MaximumLength(UsersConstants.NameMaxLength).NoNullCharacter();
        RuleFor(request => (request.Email ?? string.Empty).Trim())
            .NotEmpty()
            .OverridePropertyName(JsonNamingPolicy.CamelCase.ConvertName(nameof(RegisterRequest.Email)))
            .EmailAddress()
            .MaximumLength(UsersConstants.EmailMaxLength)
            .NoNullCharacter();
        RuleFor(request => request.Password).ValidNewPassword();
    }
}
