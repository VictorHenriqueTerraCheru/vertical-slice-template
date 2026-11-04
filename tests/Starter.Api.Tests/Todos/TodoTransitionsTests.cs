using Starter.Api.Todos;

namespace Starter.Api.Tests.Todos;

public sealed class TodoTransitionsTests
{
    private static readonly (TodoStatus From, TodoStatus To)[] Allowed =
    [
        (TodoStatus.Pending, TodoStatus.InProgress),
        (TodoStatus.Pending, TodoStatus.Canceled),
        (TodoStatus.InProgress, TodoStatus.Done),
        (TodoStatus.InProgress, TodoStatus.Pending),
        (TodoStatus.InProgress, TodoStatus.Canceled),
    ];

    public static TheoryData<TodoStatus, TodoStatus> AllowedPairs() => ToData(Allowed);

    public static TheoryData<TodoStatus, TodoStatus> RefusedPairs() => ToData(
        Enum.GetValues<TodoStatus>()
            .SelectMany(from => Enum.GetValues<TodoStatus>().Select(to => (from, to)))
            .Except(Allowed));

    private static TheoryData<TodoStatus, TodoStatus> ToData(IEnumerable<(TodoStatus From, TodoStatus To)> pairs)
    {
        var data = new TheoryData<TodoStatus, TodoStatus>();
        foreach (var (from, to) in pairs)
        {
            data.Add(from, to);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(AllowedPairs))]
    public void CanMove_accepts_the_allowed_pairs(TodoStatus from, TodoStatus to) =>
        TodoTransitions.CanMove(from, to).Should().BeTrue();

    [Theory]
    [MemberData(nameof(RefusedPairs))]
    public void CanMove_refuses_every_other_pair_including_equal_ones(TodoStatus from, TodoStatus to) =>
        TodoTransitions.CanMove(from, to).Should().BeFalse();

    [Fact]
    public void Allowed_and_refused_pairs_cover_all_sixteen_combinations() =>
        (Allowed.Length + RefusedPairs().Count).Should().Be(16);

    [Theory]
    [InlineData(TodoStatus.Pending, true)]
    [InlineData(TodoStatus.InProgress, true)]
    [InlineData(TodoStatus.Done, false)]
    [InlineData(TodoStatus.Canceled, false)]
    public void IsEditable_is_true_only_for_open_statuses(TodoStatus status, bool expected) =>
        TodoTransitions.IsEditable(status).Should().Be(expected);
}
