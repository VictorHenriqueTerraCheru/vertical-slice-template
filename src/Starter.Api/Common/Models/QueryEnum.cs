namespace Starter.Api.Common;

public readonly record struct QueryEnum<T>(T Value) : IParsable<QueryEnum<T>>
    where T : struct, Enum
{
    public static QueryEnum<T> Parse(string s, IFormatProvider? provider) =>
        TryParse(s, provider, out var result) ? result : throw new FormatException($"'{s}' is not a valid {typeof(T).Name}.");

    public static bool TryParse(string? s, IFormatProvider? provider, out QueryEnum<T> result)
    {
        var name = Enum.GetNames<T>().FirstOrDefault(candidate => string.Equals(candidate, s, StringComparison.OrdinalIgnoreCase));
        result = name is null ? default : new QueryEnum<T>(Enum.Parse<T>(name));
        return name is not null;
    }
}
