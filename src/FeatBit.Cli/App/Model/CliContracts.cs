namespace FeatBit.Cli.App.Model;

internal enum CommandKind
{
    ProjectList,
    ProjectGet,
    FlagList,
    FlagToggle,
    FlagArchive,
    FlagCreate,
    FlagSetRollout,
    FlagEvaluate,
    ConfigSet,
    ConfigInit,
    ConfigValidate,
    ConfigShow,
    ConfigClear
}

internal sealed class CliOptions
{
    public string? Host { get; set; }

    public string? Token { get; set; }

    public string? Organization { get; set; }

    public string? Name { get; set; }

    public int PageIndex { get; set; }

    public int PageSize { get; set; } = 10;

    public bool FetchAll { get; set; }

    public bool Json { get; set; }

    // flag write commands
    public string? FlagKey { get; set; }

    public string? FlagName { get; set; }

    public string? Description { get; set; }

    public bool? ToggleStatus { get; set; }

    public string? Rollout { get; set; }

    public string? DispatchKey { get; set; }

    // flag evaluate command
    public string? UserKey { get; set; }

    public string? UserName { get; set; }

    public string? CustomProperties { get; set; }

    public string? FlagKeys { get; set; }

    public string? Tags { get; set; }

    public string? TagFilterMode { get; set; }

    public string? EnvSecret { get; set; }

    public string? EvalHost { get; set; }
}

internal sealed record CommandRequest(CommandKind Kind, Guid? ProjectId, Guid? EnvId, CliOptions Options)
{
    public string? Host => Options.Host;

    public string? Token => Options.Token;

    public string? Organization => Options.Organization;

    public string? Name => Options.Name;

    public int PageIndex => Options.PageIndex;

    public int PageSize => Options.PageSize;

    public bool FetchAll => Options.FetchAll;

    public bool Json => Options.Json;

    public string? FlagKey => Options.FlagKey;

    public string? FlagName => Options.FlagName;

    public string? Description => Options.Description;

    public bool? ToggleStatus => Options.ToggleStatus;

    public string? Rollout => Options.Rollout;

    public string? DispatchKey => Options.DispatchKey;

    public string? UserKey => Options.UserKey;

    public string? UserName => Options.UserName;

    public string? CustomProperties => Options.CustomProperties;

    public string? FlagKeys => Options.FlagKeys;

    public string? Tags => Options.Tags;

    public string? TagFilterMode => Options.TagFilterMode;

    public string? EnvSecret => Options.EnvSecret;

    public string? EvalHost => Options.EvalHost;
}

internal readonly record struct ParseOutcome(bool Success, CommandRequest? Value, string? Error)
{
    public static ParseOutcome Ok(CommandRequest value) => new(true, value, null);

    public static ParseOutcome Fail(string error) => new(false, null, error);
}