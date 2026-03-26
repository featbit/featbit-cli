using FeatBit.Cli.App.Model;

namespace FeatBit.Cli.App.Parsing;

internal static class CliArgumentParser
{
    public static ParseOutcome ParseArgs(IReadOnlyList<string> args)
    {
        var options = new CliOptions();
        var positional = new List<string>(capacity: 3);

        // Named-flag equivalents of formerly positional IDs
        Guid? envId = null;
        Guid? projectId = null;

        for (var i = 0; i < args.Count; i++)
        {
            var current = args[i];

            switch (current)
            {
                case "--all":
                    options.FetchAll = true;
                    continue;
                case "--json":
                    options.Json = true;
                    continue;
                case "--dry-run":
                    options.DryRun = true;
                    continue;
                // Skip help flags — they are handled before ParseArgs is called
                case "--help":
                case "-h":
                    continue;
            }

            if (current.StartsWith("--", StringComparison.Ordinal))
            {
                if (!TryReadOptionValue(args, ref i, current, out var optionValue, out var valueError))
                {
                    return ParseOutcome.Fail(valueError!);
                }

                switch (current)
                {
                    case "--host":
                        options.Host = optionValue;
                        break;
                    case "--token":
                        options.Token = optionValue;
                        break;
                    case "--org":
                        options.Organization = optionValue;
                        break;
                    case "--name":
                        options.Name = optionValue;
                        break;
                    case "--page-index":
                        if (!int.TryParse(optionValue, out var pageIndex) || pageIndex < 0)
                        {
                            return ParseOutcome.Fail("--page-index must be an integer >= 0.");
                        }

                        options.PageIndex = pageIndex;
                        break;
                    case "--page-size":
                        if (!int.TryParse(optionValue, out var pageSize) || pageSize <= 0)
                        {
                            return ParseOutcome.Fail("--page-size must be an integer > 0.");
                        }

                        options.PageSize = pageSize;
                        break;
                    case "--env-id":
                        if (!Guid.TryParse(optionValue, out var parsedEnvId))
                            return ParseOutcome.Fail("--env-id must be a valid GUID.");
                        envId = parsedEnvId;
                        break;
                    case "--project-id":
                        if (!Guid.TryParse(optionValue, out var parsedProjectId))
                            return ParseOutcome.Fail("--project-id must be a valid GUID.");
                        projectId = parsedProjectId;
                        break;
                    case "--enabled":
                        if (!bool.TryParse(optionValue, out var enabled))
                            return ParseOutcome.Fail("--enabled must be 'true' or 'false'.");
                        options.ToggleStatus = enabled;
                        break;
                    case "--flag-key":
                        options.FlagKey = optionValue;
                        break;
                    case "--flag-name":
                        options.FlagName = optionValue;
                        break;
                    case "--description":
                        options.Description = optionValue;
                        break;
                    case "--rollout":
                        options.Rollout = optionValue;
                        break;
                    case "--dispatch-key":
                        options.DispatchKey = optionValue;
                        break;
                    case "--user-key":
                        options.UserKey = optionValue;
                        break;
                    case "--user-name":
                        options.UserName = optionValue;
                        break;
                    case "--custom-props":
                        options.CustomProperties = optionValue;
                        break;
                    case "--flag-keys":
                        options.FlagKeys = optionValue;
                        break;
                    case "--tags":
                        options.Tags = optionValue;
                        break;
                    case "--tag-filter":
                        options.TagFilterMode = optionValue;
                        break;
                    case "--env-secret":
                        options.EnvSecret = optionValue;
                        break;
                    case "--eval-host":
                        options.EvalHost = optionValue;
                        break;
                    default:
                        return ParseOutcome.Fail($"Unknown option: {current}");
                }

                continue;
            }

            positional.Add(current);
        }

        // project list
        if (positional is ["project", "list"])
        {
            return ParseOutcome.Ok(new CommandRequest(CommandKind.ProjectList, null, null, options));
        }

        // project get --project-id <guid>
        if (positional is ["project", "get"])
        {
            if (projectId is null)
                return ParseOutcome.Fail(
                    "Missing required flag: --project-id\n" +
                    "  featbit project get --project-id <guid>\n" +
                    "  Run 'featbit project list --json' to find a project ID.");
            return ParseOutcome.Ok(new CommandRequest(CommandKind.ProjectGet, projectId, null, options));
        }

        // flag list --env-id <guid>
        if (positional is ["flag", "list"])
        {
            if (envId is null)
                return ParseOutcome.Fail(
                    "Missing required flag: --env-id\n" +
                    "  featbit flag list --env-id <guid>\n" +
                    "  Run 'featbit project get --project-id <id>' to find an environment ID.");
            return ParseOutcome.Ok(new CommandRequest(CommandKind.FlagList, null, envId, options));
        }

        // flag toggle --env-id <guid> --flag-key <key> --enabled <true|false>
        if (positional is ["flag", "toggle"])
        {
            if (envId is null)
                return ParseOutcome.Fail(
                    "Missing required flag: --env-id\n" +
                    "  featbit flag toggle --env-id <guid> --flag-key <key> --enabled <true|false>");
            if (string.IsNullOrWhiteSpace(options.FlagKey))
                return ParseOutcome.Fail(
                    "Missing required flag: --flag-key\n" +
                    "  featbit flag toggle --env-id <guid> --flag-key <key> --enabled <true|false>");
            if (options.ToggleStatus is null)
                return ParseOutcome.Fail(
                    "Missing required flag: --enabled\n" +
                    "  featbit flag toggle --env-id <guid> --flag-key <key> --enabled <true|false>");
            return ParseOutcome.Ok(new CommandRequest(CommandKind.FlagToggle, null, envId, options));
        }

        // flag archive --env-id <guid> --flag-key <key>
        if (positional is ["flag", "archive"])
        {
            if (envId is null)
                return ParseOutcome.Fail(
                    "Missing required flag: --env-id\n" +
                    "  featbit flag archive --env-id <guid> --flag-key <key>");
            if (string.IsNullOrWhiteSpace(options.FlagKey))
                return ParseOutcome.Fail(
                    "Missing required flag: --flag-key\n" +
                    "  featbit flag archive --env-id <guid> --flag-key <key>");
            return ParseOutcome.Ok(new CommandRequest(CommandKind.FlagArchive, null, envId, options));
        }

        // flag create --env-id <guid> --flag-name <name> --flag-key <key>
        if (positional is ["flag", "create"])
        {
            if (envId is null)
                return ParseOutcome.Fail(
                    "Missing required flag: --env-id\n" +
                    "  featbit flag create --env-id <guid> --flag-name <name> --flag-key <key>");
            if (string.IsNullOrWhiteSpace(options.FlagName))
                return ParseOutcome.Fail(
                    "Missing required flag: --flag-name\n" +
                    "  featbit flag create --env-id <guid> --flag-name <name> --flag-key <key>");
            if (string.IsNullOrWhiteSpace(options.FlagKey))
                return ParseOutcome.Fail(
                    "Missing required flag: --flag-key\n" +
                    "  featbit flag create --env-id <guid> --flag-name <name> --flag-key <key>");
            return ParseOutcome.Ok(new CommandRequest(CommandKind.FlagCreate, null, envId, options));
        }

        // flag set-rollout --env-id <guid> --flag-key <key> --rollout <json>
        if (positional is ["flag", "set-rollout"])
        {
            if (envId is null)
                return ParseOutcome.Fail(
                    "Missing required flag: --env-id\n" +
                    "  featbit flag set-rollout --env-id <guid> --flag-key <key> --rollout <json>");
            if (string.IsNullOrWhiteSpace(options.FlagKey))
                return ParseOutcome.Fail(
                    "Missing required flag: --flag-key\n" +
                    "  featbit flag set-rollout --env-id <guid> --flag-key <key> --rollout <json>");
            if (string.IsNullOrWhiteSpace(options.Rollout))
                return ParseOutcome.Fail(
                    "Missing required flag: --rollout\n" +
                    "  featbit flag set-rollout --env-id <guid> --flag-key <key> --rollout <json>");
            return ParseOutcome.Ok(new CommandRequest(CommandKind.FlagSetRollout, null, envId, options));
        }

        // flag evaluate --user-key <key> --env-secret <secret>
        if (positional is ["flag", "evaluate"])
        {
            if (string.IsNullOrWhiteSpace(options.UserKey))
                return ParseOutcome.Fail(
                    "Missing required flag: --user-key\n" +
                    "  featbit flag evaluate --user-key <key> --env-secret <secret>");
            if (string.IsNullOrWhiteSpace(options.EnvSecret))
                return ParseOutcome.Fail(
                    "Missing required flag: --env-secret\n" +
                    "  featbit flag evaluate --user-key <key> --env-secret <secret>");
            return ParseOutcome.Ok(new CommandRequest(CommandKind.FlagEvaluate, null, null, options));
        }

        // config commands
        if (positional is ["config", "set"])
            return ParseOutcome.Ok(new CommandRequest(CommandKind.ConfigSet, null, null, options));

        if (positional is ["config", "show"])
            return ParseOutcome.Ok(new CommandRequest(CommandKind.ConfigShow, null, null, options));

        if (positional is ["config", "clear"])
            return ParseOutcome.Ok(new CommandRequest(CommandKind.ConfigClear, null, null, options));

        if (positional is ["config", "validate"])
            return ParseOutcome.Ok(new CommandRequest(CommandKind.ConfigValidate, null, null, options));

        if (positional is ["config", "init"])
            return ParseOutcome.Ok(new CommandRequest(CommandKind.ConfigInit, null, null, options));

        return ParseOutcome.Fail(
            positional.Count == 0
                ? "No command specified. Run 'featbit --help' for usage."
                : $"Unknown command: {string.Join(" ", positional)}. Run 'featbit --help' for usage.");
    }

    private static bool TryReadOptionValue(
        IReadOnlyList<string> args,
        ref int currentIndex,
        string option,
        out string value,
        out string? error)
    {
        if (currentIndex + 1 >= args.Count)
        {
            value = string.Empty;
            error = $"Missing value for {option}.";
            return false;
        }

        currentIndex++;
        value = args[currentIndex];
        error = null;
        return true;
    }
}