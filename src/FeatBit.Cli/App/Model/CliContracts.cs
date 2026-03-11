namespace FeatBit.Cli.App.Model;

internal enum CommandKind
{
    ProjectList,
    ProjectGet,
    FlagList,
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
}

internal readonly record struct ParseOutcome(bool Success, CommandRequest? Value, string? Error)
{
    public static ParseOutcome Ok(CommandRequest value) => new(true, value, null);

    public static ParseOutcome Fail(string error) => new(false, null, error);
}