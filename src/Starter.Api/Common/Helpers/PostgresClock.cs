namespace Starter.Api.Common;

public sealed class PostgresClock : TimeProvider
{
    public static DateTimeOffset Truncate(DateTimeOffset instant) =>
        new(instant.Ticks - instant.Ticks % TimeSpan.TicksPerMicrosecond, instant.Offset);

    public override DateTimeOffset GetUtcNow() => Truncate(System.GetUtcNow());
}
