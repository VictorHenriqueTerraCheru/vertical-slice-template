using FluentValidation;

namespace Starter.Api.Todos;

public sealed class TransitionTodoRequestValidator : AbstractValidator<TransitionTodoRequest>
{
    public TransitionTodoRequestValidator() =>
        RuleFor(request => request.Status).NotNull().IsInEnum();
}
