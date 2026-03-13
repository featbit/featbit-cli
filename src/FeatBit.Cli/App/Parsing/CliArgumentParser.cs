using FeatBit.Cli.App.Model;

namespace FeatBit.Cli.App.Parsing;

internal static class CliArgumentParser
{
    public static ParseOutcome ParseArgs(IReadOnlyList<string> args)
    {
        var options = new CliOptions();
        var positional = new List<string>(capacity: 4);

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

        if (positional is [var scope, var action]
            && scope.Equals("project", StringComparison.OrdinalIgnoreCase)
            && action.Equals("list", StringComparison.OrdinalIgnoreCase))
        {
            return ParseOutcome.Ok(new CommandRequest(CommandKind.ProjectList, null, null, options));
        }

        if (positional.Count == 3
            && positional[0].Equals("project", StringComparison.OrdinalIgnoreCase)
            && positional[1].Equals("get", StringComparison.OrdinalIgnoreCase))
        {
            if (!Guid.TryParse(positional[2], out var projectId))
            {
                return ParseOutcome.Fail("projectId must be a valid GUID.");
            }

            return ParseOutcome.Ok(new CommandRequest(CommandKind.ProjectGet, projectId, null, options));
        }

        if (positional.Count == 3
            && positional[0].Equals("flag", StringComparison.OrdinalIgnoreCase)
            && positional[1].Equals("list", StringComparison.OrdinalIgnoreCase))
        {
            if (!Guid.TryParse(positional[2], out var envId))
            {
                return ParseOutcome.Fail("envId must be a valid GUID.");
            }

            return ParseOutcome.Ok(new CommandRequest(CommandKind.FlagList, null, envId, options));
        }

        if (positional.Count == 2
            && positional[0].Equals("config", StringComparison.OrdinalIgnoreCase)
            && positional[1].Equals("set", StringComparison.OrdinalIgnoreCase))
        {
            return ParseOutcome.Ok(new CommandRequest(CommandKind.ConfigSet, null, null, options));
        }

        if (positional.Count == 2
            && positional[0].Equals("config", StringComparison.OrdinalIgnoreCase)
            && positional[1].Equals("show", StringComparison.OrdinalIgnoreCase))
        {
            return ParseOutcome.Ok(new CommandRequest(CommandKind.ConfigShow, null, null, options));
        }

        if (positional.Count == 2
            && positional[0].Equals("config", StringComparison.OrdinalIgnoreCase)
            && positional[1].Equals("clear", StringComparison.OrdinalIgnoreCase))
        {
            return ParseOutcome.Ok(new CommandRequest(CommandKind.ConfigClear, null, null, options));
        }

        if (positional.Count == 2
            && positional[0].Equals("config", StringComparison.OrdinalIgnoreCase)
            && positional[1].Equals("validate", StringComparison.OrdinalIgnoreCase))
        {
            return ParseOutcome.Ok(new CommandRequest(CommandKind.ConfigValidate, null, null, options));
        }

        if (positional.Count == 2
            && positional[0].Equals("config", StringComparison.OrdinalIgnoreCase)
            && positional[1].Equals("init", StringComparison.OrdinalIgnoreCase))
        {
            return ParseOutcome.Ok(new CommandRequest(CommandKind.ConfigInit, null, null, options));
        }

        // flag toggle <envId> <key> <true|false>
        if (positional.Count == 5
            && positional[0].Equals("flag", StringComparison.OrdinalIgnoreCase)
            && positional[1].Equals("toggle", StringComparison.OrdinalIgnoreCase))
        {
            if (!Guid.TryParse(positional[2], out var envId))
                return ParseOutcome.Fail("envId must be a valid GUID.");
            if (!bool.TryParse(positional[4], out var status))
                return ParseOutcome.Fail("status must be 'true' or 'false'.");
            options.FlagKey = positional[3];
            options.ToggleStatus = status;
            return ParseOutcome.Ok(new CommandRequest(CommandKind.FlagToggle, null, envId, options));
        }

        // flag archive <envId> <key>
        if (positional.Count == 4
            && positional[0].Equals("flag", StringComparison.OrdinalIgnoreCase)
            && positional[1].Equals("archive", StringComparison.OrdinalIgnoreCase))
        {
            if (!Guid.TryParse(positional[2], out var envId))
                return ParseOutcome.Fail("envId must be a valid GUID.");
            options.FlagKey = positional[3];
            return ParseOutcome.Ok(new CommandRequest(CommandKind.FlagArchive, null, envId, options));
        }

        // flag create <envId> --flag-name <name> --flag-key <key> [--description <desc>]
        if (positional.Count == 3
            && positional[0].Equals("flag", StringComparison.OrdinalIgnoreCase)
            && positional[1].Equals("create", StringComparison.OrdinalIgnoreCase))
        {
            if (!Guid.TryParse(positional[2], out var envId))
                return ParseOutcome.Fail("envId must be a valid GUID.");
            if (string.IsNullOrWhiteSpace(options.FlagName))
                return ParseOutcome.Fail("--flag-name is required for 'flag create'.");
            if (string.IsNullOrWhiteSpace(options.FlagKey))
                return ParseOutcome.Fail("--flag-key is required for 'flag create'.");
            return ParseOutcome.Ok(new CommandRequest(CommandKind.FlagCreate, null, envId, options));
        }

        // flag set-rollout <envId> <key> --rollout <json> [--dispatch-key <attr>]
        if (positional.Count == 4
            && positional[0].Equals("flag", StringComparison.OrdinalIgnoreCase)
            && positional[1].Equals("set-rollout", StringComparison.OrdinalIgnoreCase))
        {
            if (!Guid.TryParse(positional[2], out var envId))
                return ParseOutcome.Fail("envId must be a valid GUID.");
            if (string.IsNullOrWhiteSpace(options.Rollout))
                return ParseOutcome.Fail("--rollout is required for 'flag set-rollout'.");
            options.FlagKey = positional[3];
            return ParseOutcome.Ok(new CommandRequest(CommandKind.FlagSetRollout, null, envId, options));
        }

        // flag evaluate --user-key <keyId> [options...]
        if (positional.Count == 2
            && positional[0].Equals("flag", StringComparison.OrdinalIgnoreCase)
            && positional[1].Equals("evaluate", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(options.UserKey))
                return ParseOutcome.Fail("--user-key is required for 'flag evaluate'.");
            if (string.IsNullOrWhiteSpace(options.EnvSecret))
                return ParseOutcome.Fail("--env-secret is required for 'flag evaluate'.");
            return ParseOutcome.Ok(new CommandRequest(CommandKind.FlagEvaluate, null, null, options));
        }

        return ParseOutcome.Fail("Unsupported command.");
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