using FeatBit.Cli.Api;
using FeatBit.Cli.App.Config;
using FeatBit.Cli.App.Model;
using FeatBit.Cli.App.Parsing;
using FeatBit.Cli.Commands;
using FeatBit.Cli.Configuration;

namespace FeatBit.Cli.App;

internal static class CliApplication
{
    public const int ExitCodeSuccess = 0;
    public const int ExitCodeGeneralFailure = 1;
    public const int ExitCodeAuthFailure = 2;
    public const int ExitCodeNetworkFailure = 3;

    public static async Task<int> RunAsync(string[] args)
    {
        // No args → global help
        if (args.Length == 0)
        {
            CliHelpPrinter.PrintGlobal();
            return ExitCodeSuccess;
        }

        // Scoped help: intercept --help / -h at any position
        if (args.Any(static x => x is "--help" or "-h"))
        {
            var nonHelpArgs = args.Where(static x => x is not "--help" and not "-h").ToArray();
            switch (nonHelpArgs)
            {
                case []:
                    CliHelpPrinter.PrintGlobal();
                    break;
                case [var resource]:
                    CliHelpPrinter.PrintResourceHelp(resource);
                    break;
                case [var resource, var command]:
                    CliHelpPrinter.PrintCommandHelp(resource, command);
                    break;
                default:
                    CliHelpPrinter.PrintGlobal();
                    break;
            }
            return ExitCodeSuccess;
        }

        // Resource with no subcommand → show resource help
        if (args.Length == 1 && args[0] is "flag" or "project" or "config")
        {
            CliHelpPrinter.PrintResourceHelp(args[0]);
            return ExitCodeSuccess;
        }

        var parseResult = CliArgumentParser.ParseArgs(args);
        if (!parseResult.Success)
        {
            Console.Error.WriteLine($"Error: {parseResult.Error}");
            return ExitCodeGeneralFailure;
        }

        var request = parseResult.Value!;
        if (IsConfigCommand(request.Kind))
        {
            return await CliConfigCommands.ExecuteAsync(request);
        }

        // FlagEvaluate uses the env secret for auth — no admin token required.
        // Resolve only the host (eval host falls back to the main host).
        if (request.Kind == CommandKind.FlagEvaluate)
        {
            var evalHost = !string.IsNullOrWhiteSpace(request.EvalHost)
                ? request.EvalHost
                : !string.IsNullOrWhiteSpace(request.Host)
                    ? request.Host
                    : Environment.GetEnvironmentVariable("FEATBIT_EVAL_HOST")
                      ?? Environment.GetEnvironmentVariable("FEATBIT_HOST")
                      ?? OptionResolver.DefaultHost;

            using var evalHttpClient = new HttpClient();
            // Dummy token — FeatBitClient requires it but evaluate uses X-FeatBit-Env-Secret via absolute URL.
            IFeatBitClient evalClient = new FeatBitClient(evalHttpClient, evalHost, "dummy", null);
            return await CommandExecutors.FlagEvaluateAsync(
                evalClient,
                evalHost,
                request.EnvSecret!,
                request.UserKey!,
                request.UserName,
                request.CustomProperties,
                request.FlagKeys,
                request.Tags,
                request.TagFilterMode,
                request.Json,
                Console.Out,
                Console.Error,
                CancellationToken.None);
        }

        ResolvedOptions resolved;
        try
        {
            resolved = OptionResolver.Resolve(request.Host, request.Token, request.Organization, request.Json);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return ExitCodeGeneralFailure;
        }

        using var httpClient = new HttpClient();
        IFeatBitClient client = new FeatBitClient(httpClient, resolved.Host, resolved.Token, resolved.Organization);

        return await ExecuteCommandAsync(request, resolved, client);
    }

    private static bool IsConfigCommand(CommandKind kind)
    {
        return kind is CommandKind.ConfigSet
            or CommandKind.ConfigInit
            or CommandKind.ConfigValidate
            or CommandKind.ConfigShow
            or CommandKind.ConfigClear;
    }

    private static Task<int> ExecuteCommandAsync(CommandRequest request, ResolvedOptions resolved, IFeatBitClient client)
    {
        return request.Kind switch
        {
            CommandKind.ProjectList => CommandExecutors.ProjectListAsync(
                client,
                resolved.Json,
                Console.Out,
                Console.Error,
                CancellationToken.None),

            CommandKind.ProjectGet => CommandExecutors.ProjectGetAsync(
                client,
                request.ProjectId!.Value,
                resolved.Json,
                Console.Out,
                Console.Error,
                CancellationToken.None),

            CommandKind.FlagList => CommandExecutors.FlagListAsync(
                client,
                request.EnvId!.Value,
                request.Name,
                request.PageIndex,
                request.PageSize,
                request.FetchAll,
                resolved.Json,
                Console.Out,
                Console.Error,
                CancellationToken.None),

            CommandKind.FlagToggle => CommandExecutors.FlagToggleAsync(
                client,
                request.EnvId!.Value,
                request.FlagKey!,
                request.ToggleStatus!.Value,
                request.DryRun,
                resolved.Json,
                Console.Out,
                Console.Error,
                CancellationToken.None),

            CommandKind.FlagArchive => CommandExecutors.FlagArchiveAsync(
                client,
                request.EnvId!.Value,
                request.FlagKey!,
                request.DryRun,
                resolved.Json,
                Console.Out,
                Console.Error,
                CancellationToken.None),

            CommandKind.FlagCreate => CommandExecutors.FlagCreateAsync(
                client,
                request.EnvId!.Value,
                request.FlagName!,
                request.FlagKey!,
                request.Description,
                resolved.Json,
                Console.Out,
                Console.Error,
                CancellationToken.None),

            CommandKind.FlagSetRollout => CommandExecutors.FlagSetRolloutAsync(
                client,
                request.EnvId!.Value,
                request.FlagKey!,
                request.Rollout!,
                request.DispatchKey,
                resolved.Json,
                Console.Out,
                Console.Error,
                CancellationToken.None),

            _ => Task.FromResult(ExitCodeGeneralFailure)
        };
    }
}