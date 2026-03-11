using FeatBit.Cli.Api;
using FeatBit.Cli.App.Model;
using FeatBit.Cli.Configuration;

namespace FeatBit.Cli.App.Config;

internal static class CliConfigCommands
{
    public static async Task<int> ExecuteAsync(CommandRequest request)
    {
        return request.Kind switch
        {
            CommandKind.ConfigSet => ExecuteConfigSet(request),
            CommandKind.ConfigShow => ExecuteConfigShow(),
            CommandKind.ConfigClear => ExecuteConfigClear(),
            CommandKind.ConfigInit => ExecuteConfigInit(),
            CommandKind.ConfigValidate => await ExecuteConfigValidateAsync(request),
            _ => CliApplication.ExitCodeGeneralFailure
        };
    }

    private static async Task<int> ExecuteConfigValidateAsync(CommandRequest request)
    {
        ResolvedOptions resolved;
        try
        {
            resolved = OptionResolver.Resolve(request.Host, request.Token, request.Organization, json: false);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return CliApplication.ExitCodeGeneralFailure;
        }

        using var httpClient = new HttpClient();
        IFeatBitClient client = new FeatBitClient(httpClient, resolved.Host, resolved.Token, resolved.Organization);

        var response = await client.GetProjectsAsync(CancellationToken.None);
        if (!response.Success)
        {
            Console.Error.WriteLine("Config validation failed.");
            if (response.Errors is { Count: > 0 })
            {
                foreach (var error in response.Errors)
                {
                    Console.Error.WriteLine(error);
                }
            }

            return ClassifyValidationFailure(response.Errors);
        }

        var count = response.Data?.Count ?? 0;
        Console.WriteLine("Config validation succeeded.");
        Console.WriteLine($"Host: {resolved.Host}");
        Console.WriteLine($"Organization: {resolved.Organization ?? "<empty>"}");
        Console.WriteLine($"Projects fetched: {count}");
        return CliApplication.ExitCodeSuccess;
    }

    private static int ClassifyValidationFailure(IReadOnlyCollection<string>? errors)
    {
        if (errors is null || errors.Count == 0)
        {
            return CliApplication.ExitCodeGeneralFailure;
        }

        var combined = string.Join("\n", errors).ToLowerInvariant();

        if (combined.Contains("401", StringComparison.Ordinal)
            || combined.Contains("403", StringComparison.Ordinal)
            || combined.Contains("unauthorized", StringComparison.Ordinal)
            || combined.Contains("forbidden", StringComparison.Ordinal))
        {
            return CliApplication.ExitCodeAuthFailure;
        }

        if (combined.Contains("name or service not known", StringComparison.Ordinal)
            || combined.Contains("no such host", StringComparison.Ordinal)
            || combined.Contains("connection refused", StringComparison.Ordinal)
            || combined.Contains("timed out", StringComparison.Ordinal)
            || combined.Contains("network", StringComparison.Ordinal)
            || combined.Contains("dns", StringComparison.Ordinal))
        {
            return CliApplication.ExitCodeNetworkFailure;
        }

        return CliApplication.ExitCodeGeneralFailure;
    }

    private static int ExecuteConfigInit()
    {
        var config = UserConfigStore.Load();

        Console.WriteLine("Initialize FeatBit CLI user config");
        Console.WriteLine("Press Enter to keep the current value.");
        Console.WriteLine();

        var host = Prompt("Host", config.Host ?? OptionResolver.DefaultHost);
        var token = Prompt("Access token", config.Token);
        var organization = Prompt("Organization", config.Organization);

        if (string.IsNullOrWhiteSpace(host))
        {
            host = OptionResolver.DefaultHost;
        }

        if (!Uri.TryCreate(host, UriKind.Absolute, out _))
        {
            Console.Error.WriteLine("Host must be a valid absolute URL.");
            return CliApplication.ExitCodeGeneralFailure;
        }

        config.Host = host.Trim();
        config.Token = string.IsNullOrWhiteSpace(token) ? null : token.Trim();
        config.Organization = string.IsNullOrWhiteSpace(organization) ? null : organization.Trim();

        UserConfigStore.Save(config);
        Console.WriteLine($"Config saved to: {UserConfigStore.GetConfigPath()}");
        return CliApplication.ExitCodeSuccess;
    }

    private static string? Prompt(string label, string? currentValue)
    {
        var shownValue = string.IsNullOrWhiteSpace(currentValue)
            ? "<empty>"
            : label.Contains("token", StringComparison.OrdinalIgnoreCase)
                ? MaskToken(currentValue)
                : currentValue;

        Console.Write($"{label} [{shownValue}]: ");
        var input = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(input))
        {
            return currentValue;
        }

        return input;
    }

    private static int ExecuteConfigSet(CommandRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Host)
            && string.IsNullOrWhiteSpace(request.Token)
            && string.IsNullOrWhiteSpace(request.Organization))
        {
            Console.Error.WriteLine("config set requires at least one of --host, --token, or --org.");
            return CliApplication.ExitCodeGeneralFailure;
        }

        var config = UserConfigStore.Load();

        if (!string.IsNullOrWhiteSpace(request.Host))
        {
            config.Host = request.Host.Trim();
        }

        if (!string.IsNullOrWhiteSpace(request.Token))
        {
            config.Token = request.Token.Trim();
        }

        if (!string.IsNullOrWhiteSpace(request.Organization))
        {
            config.Organization = request.Organization.Trim();
        }

        UserConfigStore.Save(config);
        Console.WriteLine($"Config saved to: {UserConfigStore.GetConfigPath()}");
        return CliApplication.ExitCodeSuccess;
    }

    private static int ExecuteConfigShow()
    {
        var config = UserConfigStore.Load();
        Console.WriteLine($"Config file: {UserConfigStore.GetConfigPath()}");
        Console.WriteLine($"host: {config.Host ?? "<empty>"}");
        Console.WriteLine($"token: {MaskToken(config.Token)}");
        Console.WriteLine($"organization: {config.Organization ?? "<empty>"}");
        return CliApplication.ExitCodeSuccess;
    }

    private static int ExecuteConfigClear()
    {
        var removed = UserConfigStore.Clear();
        Console.WriteLine(removed ? "Config cleared." : "Config file not found.");
        return CliApplication.ExitCodeSuccess;
    }

    private static string MaskToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return "<empty>";
        }

        var normalizedToken = token.Trim();
        if (normalizedToken.Length <= 8)
        {
            return "***";
        }

        return $"{normalizedToken[..4]}...{normalizedToken[^4..]}";
    }
}