using FeatBit.Cli.Configuration;

namespace FeatBit.Cli.App;

internal static class CliHelpPrinter
{
    private const string Indent = "  ";
    private const string ExampleGuid = "3f6a0e24-aaa0-4b6e-9c1d-1234567890ab";

    // ── Top-level ──────────────────────────────────────────────────────────────

    public static void PrintGlobal()
    {
        Console.WriteLine($"FeatBit CLI — manage feature flags via the FeatBit API.");
        Console.WriteLine($"Default host: {OptionResolver.DefaultHost}");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine($"{Indent}featbit <resource> <command> [flags]");
        Console.WriteLine();
        Console.WriteLine("Resources:");
        Console.WriteLine($"{Indent}flag      Manage feature flags");
        Console.WriteLine($"{Indent}project   Manage projects");
        Console.WriteLine($"{Indent}config    Manage CLI configuration");
        Console.WriteLine();
        Console.WriteLine("Run 'featbit <resource> --help' for resource commands.");
        Console.WriteLine("Run 'featbit <resource> <command> --help' for flags and examples.");
    }

    // ── Resource-level help ────────────────────────────────────────────────────

    public static void PrintResourceHelp(string resource)
    {
        switch (resource.ToLowerInvariant())
        {
            case "flag":
                PrintFlagHelp();
                break;
            case "project":
                PrintProjectHelp();
                break;
            case "config":
                PrintConfigHelp();
                break;
            default:
                PrintGlobal();
                break;
        }
    }

    public static void PrintFlagHelp()
    {
        Console.WriteLine("Manage feature flags.");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine($"{Indent}featbit flag <command> [flags]");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine($"{Indent}list        List feature flags in an environment");
        Console.WriteLine($"{Indent}create      Create a new feature flag");
        Console.WriteLine($"{Indent}toggle      Enable or disable a feature flag");
        Console.WriteLine($"{Indent}archive     Archive a feature flag");
        Console.WriteLine($"{Indent}set-rollout Update the default rollout for a feature flag");
        Console.WriteLine($"{Indent}evaluate    Evaluate feature flags for a user");
        Console.WriteLine();
        Console.WriteLine("Run 'featbit flag <command> --help' for flags and examples.");
    }

    public static void PrintProjectHelp()
    {
        Console.WriteLine("Manage FeatBit projects.");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine($"{Indent}featbit project <command> [flags]");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine($"{Indent}list   List all accessible projects");
        Console.WriteLine($"{Indent}get    Get project details and environments");
        Console.WriteLine();
        Console.WriteLine("Run 'featbit project <command> --help' for flags and examples.");
    }

    public static void PrintConfigHelp()
    {
        Console.WriteLine("Manage CLI configuration (host, token, organization).");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine($"{Indent}featbit config <command> [flags]");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine($"{Indent}init      Initialize config interactively (requires a TTY)");
        Console.WriteLine($"{Indent}set       Set one or more config values non-interactively");
        Console.WriteLine($"{Indent}show      Display current config");
        Console.WriteLine($"{Indent}validate  Test API connectivity with the current config");
        Console.WriteLine($"{Indent}clear     Delete the config file");
        Console.WriteLine();
        Console.WriteLine("Run 'featbit config <command> --help' for flags and examples.");
    }

    // ── Command-level help ─────────────────────────────────────────────────────

    public static void PrintCommandHelp(string resource, string command)
    {
        var key = $"{resource.ToLowerInvariant()} {command.ToLowerInvariant()}";
        switch (key)
        {
            case "flag list":       PrintFlagListHelp();       break;
            case "flag create":     PrintFlagCreateHelp();     break;
            case "flag toggle":     PrintFlagToggleHelp();     break;
            case "flag archive":    PrintFlagArchiveHelp();    break;
            case "flag set-rollout":PrintFlagSetRolloutHelp(); break;
            case "flag evaluate":   PrintFlagEvaluateHelp();   break;
            case "project list":    PrintProjectListHelp();    break;
            case "project get":     PrintProjectGetHelp();     break;
            case "config init":     PrintConfigInitHelp();     break;
            case "config set":      PrintConfigSetHelp();      break;
            case "config show":     PrintConfigShowHelp();     break;
            case "config validate": PrintConfigValidateHelp(); break;
            case "config clear":    PrintConfigClearHelp();    break;
            default:                PrintResourceHelp(resource); break;
        }
    }

    // flag list
    public static void PrintFlagListHelp()
    {
        Console.WriteLine("List feature flags in an environment.");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine($"{Indent}featbit flag list --env-id <guid> [flags]");
        Console.WriteLine();
        Console.WriteLine("Flags:");
        Console.WriteLine($"{Indent}--env-id <guid>     Environment ID (required)");
        Console.WriteLine($"{Indent}--name <string>     Filter by name or key fragment");
        Console.WriteLine($"{Indent}--page-index <int>  Page index, 0-based (default: 0)");
        Console.WriteLine($"{Indent}--page-size <int>   Items per page (default: 10)");
        Console.WriteLine($"{Indent}--all               Fetch all pages");
        Console.WriteLine($"{Indent}--json              Output as JSON");
        Console.WriteLine($"{Indent}--host <url>        API host (overrides config)");
        Console.WriteLine($"{Indent}--token <token>     Access token (overrides config)");
        Console.WriteLine($"{Indent}--org <orgId>       Organization ID (overrides config)");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine($"{Indent}featbit flag list --env-id {ExampleGuid}");
        Console.WriteLine($"{Indent}featbit flag list --env-id {ExampleGuid} --name my-flag --all --json");
    }

    // flag create
    public static void PrintFlagCreateHelp()
    {
        Console.WriteLine("Create a new boolean feature flag.");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine($"{Indent}featbit flag create --env-id <guid> --flag-name <name> --flag-key <key> [flags]");
        Console.WriteLine();
        Console.WriteLine("Flags:");
        Console.WriteLine($"{Indent}--env-id <guid>          Environment ID (required)");
        Console.WriteLine($"{Indent}--flag-name <string>     Display name of the flag (required)");
        Console.WriteLine($"{Indent}--flag-key <string>      Unique key of the flag (required)");
        Console.WriteLine($"{Indent}--description <string>   Optional description");
        Console.WriteLine($"{Indent}--json                   Output as JSON");
        Console.WriteLine($"{Indent}--host <url>             API host (overrides config)");
        Console.WriteLine($"{Indent}--token <token>          Access token (overrides config)");
        Console.WriteLine($"{Indent}--org <orgId>            Organization ID (overrides config)");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine($"{Indent}featbit flag create --env-id {ExampleGuid} --flag-name \"My Flag\" --flag-key my-flag");
        Console.WriteLine($"{Indent}featbit flag create --env-id {ExampleGuid} --flag-name \"My Flag\" --flag-key my-flag --description \"A test flag\" --json");
    }

    // flag toggle
    public static void PrintFlagToggleHelp()
    {
        Console.WriteLine("Enable or disable a feature flag.");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine($"{Indent}featbit flag toggle --env-id <guid> --flag-key <key> --enabled <true|false> [flags]");
        Console.WriteLine();
        Console.WriteLine("Flags:");
        Console.WriteLine($"{Indent}--env-id <guid>      Environment ID (required)");
        Console.WriteLine($"{Indent}--flag-key <string>  Feature flag key (required)");
        Console.WriteLine($"{Indent}--enabled <bool>     true to enable, false to disable (required)");
        Console.WriteLine($"{Indent}--dry-run            Show what would happen without making changes");
        Console.WriteLine($"{Indent}--json               Output as JSON");
        Console.WriteLine($"{Indent}--host <url>         API host (overrides config)");
        Console.WriteLine($"{Indent}--token <token>      Access token (overrides config)");
        Console.WriteLine($"{Indent}--org <orgId>        Organization ID (overrides config)");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine($"{Indent}featbit flag toggle --env-id {ExampleGuid} --flag-key my-flag --enabled true");
        Console.WriteLine($"{Indent}featbit flag toggle --env-id {ExampleGuid} --flag-key my-flag --enabled false --dry-run");
    }

    // flag archive
    public static void PrintFlagArchiveHelp()
    {
        Console.WriteLine("Archive a feature flag.");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine($"{Indent}featbit flag archive --env-id <guid> --flag-key <key> [flags]");
        Console.WriteLine();
        Console.WriteLine("Flags:");
        Console.WriteLine($"{Indent}--env-id <guid>      Environment ID (required)");
        Console.WriteLine($"{Indent}--flag-key <string>  Feature flag key (required)");
        Console.WriteLine($"{Indent}--dry-run            Show what would happen without making changes");
        Console.WriteLine($"{Indent}--json               Output as JSON");
        Console.WriteLine($"{Indent}--host <url>         API host (overrides config)");
        Console.WriteLine($"{Indent}--token <token>      Access token (overrides config)");
        Console.WriteLine($"{Indent}--org <orgId>        Organization ID (overrides config)");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine($"{Indent}featbit flag archive --env-id {ExampleGuid} --flag-key my-flag");
        Console.WriteLine($"{Indent}featbit flag archive --env-id {ExampleGuid} --flag-key my-flag --dry-run");
    }

    // flag set-rollout
    public static void PrintFlagSetRolloutHelp()
    {
        Console.WriteLine("Update the default rollout for a feature flag.");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine($"{Indent}featbit flag set-rollout --env-id <guid> --flag-key <key> --rollout <json> [flags]");
        Console.WriteLine();
        Console.WriteLine("Flags:");
        Console.WriteLine($"{Indent}--env-id <guid>          Environment ID (required)");
        Console.WriteLine($"{Indent}--flag-key <string>      Feature flag key (required)");
        Console.WriteLine($"{Indent}--rollout <json>         Rollout assignments as JSON (required)");
        Console.WriteLine($"{Indent}                         Format: '[{{\"variationId\":\"<uuid>\",\"percentage\":<n>}},...]'");
        Console.WriteLine($"{Indent}                         Percentages must sum to 100");
        Console.WriteLine($"{Indent}--dispatch-key <string>  Attribute used to hash users into buckets");
        Console.WriteLine($"{Indent}--json                   Output as JSON");
        Console.WriteLine($"{Indent}--host <url>             API host (overrides config)");
        Console.WriteLine($"{Indent}--token <token>          Access token (overrides config)");
        Console.WriteLine($"{Indent}--org <orgId>            Organization ID (overrides config)");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine($"{Indent}featbit flag set-rollout --env-id {ExampleGuid} --flag-key my-flag --rollout '[{{\"variationId\":\"uuid1\",\"percentage\":50}},{{\"variationId\":\"uuid2\",\"percentage\":50}}]'");
        Console.WriteLine($"{Indent}featbit flag set-rollout --env-id {ExampleGuid} --flag-key my-flag --rollout '[{{\"variationId\":\"uuid1\",\"percentage\":100}}]' --dispatch-key email");
    }

    // flag evaluate
    public static void PrintFlagEvaluateHelp()
    {
        Console.WriteLine("Evaluate feature flags for a user.");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine($"{Indent}featbit flag evaluate --user-key <key> --env-secret <secret> [flags]");
        Console.WriteLine();
        Console.WriteLine("Flags:");
        Console.WriteLine($"{Indent}--user-key <string>      User key to evaluate for (required)");
        Console.WriteLine($"{Indent}--env-secret <string>    Environment secret key (required)");
        Console.WriteLine($"{Indent}--user-name <string>     Optional user display name");
        Console.WriteLine($"{Indent}--custom-props <json>    Optional custom user properties as JSON");
        Console.WriteLine($"{Indent}--flag-keys <keys>       Comma-separated flag keys to evaluate");
        Console.WriteLine($"{Indent}--tags <tags>            Comma-separated tags to filter flags by");
        Console.WriteLine($"{Indent}--tag-filter <mode>      Tag filter mode: 'and' or 'or' (default: or)");
        Console.WriteLine($"{Indent}--eval-host <url>        Evaluation service URL (defaults to --host)");
        Console.WriteLine($"{Indent}--host <url>             Main API host (overrides config)");
        Console.WriteLine($"{Indent}--json                   Output as JSON");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine($"{Indent}featbit flag evaluate --user-key user@example.com --env-secret abc123");
        Console.WriteLine($"{Indent}featbit flag evaluate --user-key user@example.com --env-secret abc123 --flag-keys flag1,flag2 --json");
    }

    // project list
    public static void PrintProjectListHelp()
    {
        Console.WriteLine("List all accessible projects.");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine($"{Indent}featbit project list [flags]");
        Console.WriteLine();
        Console.WriteLine("Flags:");
        Console.WriteLine($"{Indent}--json           Output as JSON");
        Console.WriteLine($"{Indent}--host <url>     API host (overrides config)");
        Console.WriteLine($"{Indent}--token <token>  Access token (overrides config)");
        Console.WriteLine($"{Indent}--org <orgId>    Organization ID (overrides config)");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine($"{Indent}featbit project list");
        Console.WriteLine($"{Indent}featbit project list --json");
    }

    // project get
    public static void PrintProjectGetHelp()
    {
        Console.WriteLine("Get project details and list its environments.");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine($"{Indent}featbit project get --project-id <guid> [flags]");
        Console.WriteLine();
        Console.WriteLine("Flags:");
        Console.WriteLine($"{Indent}--project-id <guid>  Project ID (required)");
        Console.WriteLine($"{Indent}--json               Output as JSON");
        Console.WriteLine($"{Indent}--host <url>         API host (overrides config)");
        Console.WriteLine($"{Indent}--token <token>      Access token (overrides config)");
        Console.WriteLine($"{Indent}--org <orgId>        Organization ID (overrides config)");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine($"{Indent}featbit project get --project-id {ExampleGuid}");
        Console.WriteLine($"{Indent}featbit project get --project-id {ExampleGuid} --json");
    }

    // config init
    public static void PrintConfigInitHelp()
    {
        Console.WriteLine("Initialize CLI configuration interactively.");
        Console.WriteLine("Requires an interactive terminal (TTY). Use 'config set' for non-interactive use.");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine($"{Indent}featbit config init");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine($"{Indent}featbit config init");
        Console.WriteLine($"{Indent}featbit config set --host https://your-api.example.com --token <token>");
    }

    // config set
    public static void PrintConfigSetHelp()
    {
        Console.WriteLine("Set one or more configuration values non-interactively.");
        Console.WriteLine("At least one flag must be provided.");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine($"{Indent}featbit config set [--host <url>] [--token <token>] [--org <orgId>]");
        Console.WriteLine();
        Console.WriteLine("Flags:");
        Console.WriteLine($"{Indent}--host <url>     API host URL");
        Console.WriteLine($"{Indent}--token <token>  Access token");
        Console.WriteLine($"{Indent}--org <orgId>    Organization ID");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine($"{Indent}featbit config set --host https://api.featbit.co --token mytoken");
        Console.WriteLine($"{Indent}featbit config set --org 00000000-0000-0000-0000-000000000001");
    }

    // config show
    public static void PrintConfigShowHelp()
    {
        Console.WriteLine("Display current CLI configuration.");
        Console.WriteLine("The token value is masked for security.");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine($"{Indent}featbit config show");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine($"{Indent}featbit config show");
    }

    // config validate
    public static void PrintConfigValidateHelp()
    {
        Console.WriteLine("Test API connectivity with the current configuration.");
        Console.WriteLine();
        Console.WriteLine("Exit codes:");
        Console.WriteLine($"{Indent}0  Connection and authentication succeeded");
        Console.WriteLine($"{Indent}1  General failure");
        Console.WriteLine($"{Indent}2  Authentication failure (401 / 403)");
        Console.WriteLine($"{Indent}3  Network failure (DNS / timeout / connection refused)");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine($"{Indent}featbit config validate [--host <url>] [--token <token>] [--org <orgId>]");
        Console.WriteLine();
        Console.WriteLine("Flags:");
        Console.WriteLine($"{Indent}--host <url>     API host (overrides config)");
        Console.WriteLine($"{Indent}--token <token>  Access token (overrides config)");
        Console.WriteLine($"{Indent}--org <orgId>    Organization ID (overrides config)");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine($"{Indent}featbit config validate");
        Console.WriteLine($"{Indent}featbit config validate --host https://api.example.com --token mytoken");
    }

    // config clear
    public static void PrintConfigClearHelp()
    {
        Console.WriteLine("Delete the CLI configuration file.");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine($"{Indent}featbit config clear");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine($"{Indent}featbit config clear");
    }
}
