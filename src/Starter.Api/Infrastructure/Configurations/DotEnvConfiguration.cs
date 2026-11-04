namespace Starter.Api.Infrastructure;

public static class DotEnvConfiguration
{
    public static void AddDotEnvFile(this ConfigurationManager configuration, string startDirectory)
    {
        var path = Locate(startDirectory);
        if (path is null)
        {
            return;
        }

        var values = new Dictionary<string, string?>();
        foreach (var line in File.ReadLines(path))
        {
            var entry = Parse(line);
            if (entry is { } pair && string.IsNullOrWhiteSpace(configuration[pair.Key]))
            {
                values[pair.Key] = pair.Value;
            }
        }

        configuration.AddInMemoryCollection(values);
    }

    private static string? Locate(string startDirectory)
    {
        for (var directory = new DirectoryInfo(startDirectory); directory is not null; directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, ".env");
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static KeyValuePair<string, string>? Parse(string line)
    {
        var trimmed = line.Trim();
        var separator = trimmed.IndexOf('=');
        if (trimmed.StartsWith('#') || separator <= 0)
        {
            return null;
        }

        var value = trimmed[(separator + 1)..].Trim().Trim('"');
        return new KeyValuePair<string, string>(trimmed[..separator].Trim(), value);
    }
}
