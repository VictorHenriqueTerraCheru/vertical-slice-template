using FluentValidation;

namespace Starter.Api.Todos;

public sealed class CreateTodoRequestValidator : AbstractValidator<CreateTodoRequest>
{
    public CreateTodoRequestValidator() =>
        this.AddTodoTextRules(request => request.Title, request => request.Description!);
}
