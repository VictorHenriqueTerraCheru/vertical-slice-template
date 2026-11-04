using FluentValidation;
using Starter.Api.Common;

namespace Starter.Api.Users;

public sealed class UpdateMeRequestValidator : AbstractValidator<UpdateMeRequest>
{
    public UpdateMeRequestValidator()
    {
        RuleFor(request => request.Name).NotEmpty().MaximumLength(UsersConstants.NameMaxLength).NoNullCharacter();
    }
}
