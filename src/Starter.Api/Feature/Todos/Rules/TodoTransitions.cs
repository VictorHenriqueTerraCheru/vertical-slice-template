namespace Starter.Api.Todos;

public static class TodoTransitions
{
    public static bool CanMove(TodoStatus from, TodoStatus to) => (from, to) switch
    {
        (TodoStatus.Pending, TodoStatus.InProgress) => true,
        (TodoStatus.Pending, TodoStatus.Canceled) => true,
        (TodoStatus.InProgress, TodoStatus.Done) => true,
        (TodoStatus.InProgress, TodoStatus.Pending) => true,
        (TodoStatus.InProgress, TodoStatus.Canceled) => true,
        _ => false,
    };

    public static bool IsEditable(TodoStatus status) => status is TodoStatus.Pending or TodoStatus.InProgress;
}
