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
        if (args.Length == 0 || args.Any(static x => x is "--help" or "-h"))
        {
            CliHelpPrinter.Print();
            return ExitCodeSuccess;
        }

        var parseResult = CliArgumentParser.ParseArgs(args);
        if (!parseResult.Success)
        {
            Console.Error.WriteLine(parseResult.Error);
            CliHelpPrinter.Print();
            return ExitCodeGeneralFailure;
        }

        var request = parseResult.Value!;
        if (IsConfigCommand(request.Kind))
        {
            return await CliConfigCommands.ExecuteAsync(request);
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

            _ => Task.FromResult(ExitCodeGeneralFailure)
        };
    }
}