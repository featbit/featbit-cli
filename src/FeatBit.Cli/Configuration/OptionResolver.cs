namespace FeatBit.Cli.Configuration;

public static class OptionResolver
{
    public const string DefaultHost = "https://app-api.featbit.co";

    public static ResolvedOptions Resolve(string? host, string? token, string? organization, bool json)
    {
        var userConfig = UserConfigStore.Load();
        var resolvedHost = ResolveOptional(host, "FEATBIT_HOST", userConfig.Host) ?? DefaultHost;
        var resolvedToken = ResolveRequired(token, "FEATBIT_TOKEN", "token", userConfig.Token);
        var resolvedOrg = ResolveOptional(organization, "FEATBIT_ORG", userConfig.Organization);

        return new ResolvedOptions(resolvedHost, resolvedToken, resolvedOrg, json);
    }

    private static string ResolveRequired(
        string? cliValue,
        string envName,
        string displayName,
        params string?[] fallbacks)
    {
        var value = string.IsNullOrWhiteSpace(cliValue)
            ? Environment.GetEnvironmentVariable(envName)
            : cliValue;

        if (!string.IsNullOrWhiteSpace(value))
        {
            return value.Trim();
        }

        foreach (var fallback in fallbacks)
        {
            if (!string.IsNullOrWhiteSpace(fallback))
            {
                return fallback.Trim();
            }
        }

        throw new InvalidOperationException(
            $"Missing required option '{displayName}'. Use --{displayName} or set {envName}. You can also run 'featbit config set'.");
    }

    private static string? ResolveOptional(string? cliValue, string envName, params string?[] fallbacks)
    {
        var value = string.IsNullOrWhiteSpace(cliValue)
            ? Environment.GetEnvironmentVariable(envName)
            : cliValue;

        if (!string.IsNullOrWhiteSpace(value))
        {
            return value.Trim();
        }

        foreach (var fallback in fallbacks)
        {
            if (!string.IsNullOrWhiteSpace(fallback))
            {
                return fallback.Trim();
            }
        }

        return null;
    }
}

public sealed record ResolvedOptions(string Host, string Token, string? Organization, bool Json);
