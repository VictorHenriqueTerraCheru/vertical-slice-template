using FluentValidation;

namespace Starter.Api.Todos;

public sealed class UpdateTodoRequestValidator : AbstractValidator<UpdateTodoRequest>
{
    public UpdateTodoRequestValidator() =>
        this.AddTodoTextRules(request => request.Title, request => request.Description!);
}
