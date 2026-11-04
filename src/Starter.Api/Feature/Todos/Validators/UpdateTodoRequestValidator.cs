using System.Text.Json;
using FluentValidation;
using Starter.Api.Common;

namespace Starter.Api.Todos;

public sealed class UpdateTodoRequestValidator : AbstractValidator<UpdateTodoRequest>
{
    public UpdateTodoRequestValidator()
    {
        RuleFor(request => (request.Title ?? string.Empty).Trim())
            .NotEmpty()
            .OverridePropertyName(JsonNamingPolicy.CamelCase.ConvertName(nameof(UpdateTodoRequest.Title)))
            .MaximumLength(TodosConstants.TitleMaxLength)
            .NoNullCharacter();
        RuleFor(request => (request.Description ?? string.Empty).Trim())
            .MaximumLength(TodosConstants.DescriptionMaxLength)
            .OverridePropertyName(JsonNamingPolicy.CamelCase.ConvertName(nameof(UpdateTodoRequest.Description)))
            .NoNullCharacter();
    }
}
