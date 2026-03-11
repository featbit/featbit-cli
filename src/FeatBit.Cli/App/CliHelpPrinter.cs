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
        Console.WriteLine("featbit config set [--host <url>] [--token <token>] [--org <orgId>]");
        Console.WriteLine("featbit config init");
        Console.WriteLine("featbit config validate [--host <url>] [--token <token>] [--org <orgId>]");
        Console.WriteLine("validate exit codes: 0=ok, 1=general failure, 2=auth failure, 3=network failure");
        Console.WriteLine("featbit config show");
        Console.WriteLine("featbit config clear");
    }
}