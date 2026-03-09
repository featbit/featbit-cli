namespace FeatBit.Cli.Configuration;

public static class OptionResolver
{
    public static ResolvedOptions Resolve(string? host, string? token, string? organization, bool json)
    {
        var resolvedHost = ResolveRequired(host, "FEATBIT_HOST", "host");
        var resolvedToken = ResolveRequired(token, "FEATBIT_TOKEN", "token");
        var resolvedOrg = ResolveOptional(organization, "FEATBIT_ORG");

        return new ResolvedOptions(resolvedHost, resolvedToken, resolvedOrg, json);
    }

    private static string ResolveRequired(string? cliValue, string envName, string displayName)
    {
        var value = string.IsNullOrWhiteSpace(cliValue)
            ? Environment.GetEnvironmentVariable(envName)
            : cliValue;

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"Missing required option '{displayName}'. Use --{displayName} or set {envName}.");
        }

        return value.Trim();
    }

    private static string? ResolveOptional(string? cliValue, string envName)
    {
        var value = string.IsNullOrWhiteSpace(cliValue)
            ? Environment.GetEnvironmentVariable(envName)
            : cliValue;

        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}

public sealed record ResolvedOptions(string Host, string Token, string? Organization, bool Json);
