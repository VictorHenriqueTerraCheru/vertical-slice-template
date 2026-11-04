namespace Starter.Api.Common;

public static class EnvironmentNames
{
    public const string Development = "Development";
    public const string Testing = "Testing";
    public const string Staging = "Staging";
    public const string Production = "Production";

    public static readonly IReadOnlyList<string> All = [Development, Testing, Staging, Production];
}
