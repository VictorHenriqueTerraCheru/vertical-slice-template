namespace Starter.Api.Todos;

public static class TodosConstants
{
    public const int TitleMaxLength = 200;
    public const int DescriptionMaxLength = 2000;
    public const string SortByCreatedAt = "createdAt";
    public const string SortByTitle = "title";

    public static IReadOnlyList<string> SortableFields { get; } = [SortByCreatedAt, SortByTitle];
}
