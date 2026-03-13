using FeatBit.Cli.Configuration;

namespace FeatBit.Cli.App;

internal static class CliHelpPrinter
{
    public static void Print()
    {
        Console.WriteLine($"Default host: {OptionResolver.DefaultHost}");
        Console.WriteLine("featbit project list [--host <url>] [--token <token>] [--org <orgId>] [--json]");
        Console.WriteLine("featbit project get <projectId> [--host <url>] [--token <token>] [--org <orgId>] [--json]");
        Console.WriteLine("featbit flag list <envId> [--name <name>] [--page-index <n>] [--page-size <n>] [--all] [--host <url>] [--token <token>] [--org <orgId>] [--json]");
        Console.WriteLine("featbit flag toggle <envId> <key> <true|false> [--host <url>] [--token <token>] [--org <orgId>] [--json]");
        Console.WriteLine("featbit flag archive <envId> <key> [--host <url>] [--token <token>] [--org <orgId>] [--json]");
        Console.WriteLine("featbit flag create <envId> --flag-name <name> --flag-key <key> [--description <desc>] [--host <url>] [--token <token>] [--org <orgId>] [--json]");
        Console.WriteLine("featbit flag set-rollout <envId> <key> --rollout <json> [--dispatch-key <attr>] [--host <url>] [--token <token>] [--org <orgId>] [--json]");
        Console.WriteLine("  rollout json: '[{\"variationId\":\"<uuid>\",\"percentage\":<n>},...]' (percentages must sum to 100)");
        Console.WriteLine("featbit flag evaluate --user-key <keyId> --env-secret <secret> [--user-name <name>] [--custom-props <json>] [--flag-keys <k1,k2>] [--tags <t1,t2>] [--tag-filter and|or] [--eval-host <url>] [--host <url>] [--json]");
        Console.WriteLine("featbit config set [--host <url>] [--token <token>] [--org <orgId>]");
        Console.WriteLine("featbit config init");
        Console.WriteLine("featbit config validate [--host <url>] [--token <token>] [--org <orgId>]");
        Console.WriteLine("validate exit codes: 0=ok, 1=general failure, 2=auth failure, 3=network failure");
        Console.WriteLine("featbit config show");
        Console.WriteLine("featbit config clear");
    }
}