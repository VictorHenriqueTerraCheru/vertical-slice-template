using Starter.Api.Common;

namespace Starter.Api.Todos;

public static class TodoErrors
{
    public static ApplicationError InvalidTransition { get; } = ApplicationError.Rule(
        StatusCodes.Status409Conflict, "invalid-transition", "Essa mudança de status não é permitida para a tarefa.");

    public static ApplicationError NotEditable { get; } = ApplicationError.Rule(
        StatusCodes.Status409Conflict, "todo-not-editable", "Tarefas concluídas ou canceladas não podem ser editadas.");
}
