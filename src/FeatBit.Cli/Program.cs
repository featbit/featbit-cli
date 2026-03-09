using FeatBit.Cli.Api;
using FeatBit.Cli.Commands;
using FeatBit.Cli.Configuration;

const int ExitCodeSuccess = 0;
const int ExitCodeGeneralFailure = 1;
const int ExitCodeAuthFailure = 2;
const int ExitCodeNetworkFailure = 3;

if (args.Length == 0 || args.Any(static x => x is "--help" or "-h"))
{
	PrintHelp();
	return 0;
}

var parseResult = ParseArgs(args);
if (!parseResult.Success)
{
	Console.Error.WriteLine(parseResult.Error);
	PrintHelp();
	return 1;
}

var request = parseResult.Value!;

if (request.Kind is CommandKind.ConfigSet or CommandKind.ConfigInit or CommandKind.ConfigValidate or CommandKind.ConfigShow or CommandKind.ConfigClear)
{
	return await ExecuteConfigAsync(request);
}

ResolvedOptions resolved;
try
{
	resolved = OptionResolver.Resolve(request.Host, request.Token, request.Organization, request.Json);
}
catch (Exception ex)
{
	Console.Error.WriteLine(ex.Message);
	return 1;
}

using var httpClient = new HttpClient();
IFeatBitClient client = new FeatBitClient(httpClient, resolved.Host, resolved.Token, resolved.Organization);

return await ExecuteAsync(request, resolved, client);

static async Task<int> ExecuteAsync(CommandRequest request, ResolvedOptions resolved, IFeatBitClient client)
{
	return request.Kind switch
	{
		CommandKind.ProjectList => await CommandExecutors.ProjectListAsync(
			client,
			resolved.Json,
			Console.Out,
			Console.Error,
			CancellationToken.None),

		CommandKind.ProjectGet => await CommandExecutors.ProjectGetAsync(
			client,
			request.ProjectId!.Value,
			resolved.Json,
			Console.Out,
			Console.Error,
			CancellationToken.None),

		CommandKind.FlagList => await CommandExecutors.FlagListAsync(
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

		_ => 1
	};
}

static ParseOutcome ParseArgs(IReadOnlyList<string> args)
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
				default:
					return ParseOutcome.Fail($"Unknown option: {current}");
			}

			continue;
		}

		positional.Add(current);
	}

	if (positional is [var scope, var action] &&
		scope.Equals("project", StringComparison.OrdinalIgnoreCase) &&
		action.Equals("list", StringComparison.OrdinalIgnoreCase))
	{
		return ParseOutcome.Ok(new CommandRequest(CommandKind.ProjectList, null, null, options));
	}

	if (positional.Count == 3 &&
		positional[0].Equals("project", StringComparison.OrdinalIgnoreCase) &&
		positional[1].Equals("get", StringComparison.OrdinalIgnoreCase))
	{
		if (!Guid.TryParse(positional[2], out var projectId))
		{
			return ParseOutcome.Fail("projectId must be a valid GUID.");
		}

		return ParseOutcome.Ok(new CommandRequest(CommandKind.ProjectGet, projectId, null, options));
	}

	if (positional.Count == 3 &&
		positional[0].Equals("flag", StringComparison.OrdinalIgnoreCase) &&
		positional[1].Equals("list", StringComparison.OrdinalIgnoreCase))
	{
		if (!Guid.TryParse(positional[2], out var envId))
		{
			return ParseOutcome.Fail("envId must be a valid GUID.");
		}

		return ParseOutcome.Ok(new CommandRequest(CommandKind.FlagList, null, envId, options));
	}

	if (positional.Count == 2 &&
		positional[0].Equals("config", StringComparison.OrdinalIgnoreCase) &&
		positional[1].Equals("set", StringComparison.OrdinalIgnoreCase))
	{
		return ParseOutcome.Ok(new CommandRequest(CommandKind.ConfigSet, null, null, options));
	}

	if (positional.Count == 2 &&
		positional[0].Equals("config", StringComparison.OrdinalIgnoreCase) &&
		positional[1].Equals("show", StringComparison.OrdinalIgnoreCase))
	{
		return ParseOutcome.Ok(new CommandRequest(CommandKind.ConfigShow, null, null, options));
	}

	if (positional.Count == 2 &&
		positional[0].Equals("config", StringComparison.OrdinalIgnoreCase) &&
		positional[1].Equals("clear", StringComparison.OrdinalIgnoreCase))
	{
		return ParseOutcome.Ok(new CommandRequest(CommandKind.ConfigClear, null, null, options));
	}

	if (positional.Count == 2 &&
		positional[0].Equals("config", StringComparison.OrdinalIgnoreCase) &&
		positional[1].Equals("validate", StringComparison.OrdinalIgnoreCase))
	{
		return ParseOutcome.Ok(new CommandRequest(CommandKind.ConfigValidate, null, null, options));
	}

	if (positional.Count == 2 &&
		positional[0].Equals("config", StringComparison.OrdinalIgnoreCase) &&
		positional[1].Equals("init", StringComparison.OrdinalIgnoreCase))
	{
		return ParseOutcome.Ok(new CommandRequest(CommandKind.ConfigInit, null, null, options));
	}

	return ParseOutcome.Fail("Unsupported command.");
}

static bool TryReadOptionValue(
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

static void PrintHelp()
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

static async Task<int> ExecuteConfigAsync(CommandRequest request)
{
	switch (request.Kind)
	{
		case CommandKind.ConfigSet:
			return ExecuteConfigSet(request);
		case CommandKind.ConfigShow:
			return ExecuteConfigShow();
		case CommandKind.ConfigClear:
			return ExecuteConfigClear();
		case CommandKind.ConfigInit:
			return ExecuteConfigInit();
		case CommandKind.ConfigValidate:
			return await ExecuteConfigValidateAsync(request);
		default:
			return ExitCodeGeneralFailure;
	}
}

static async Task<int> ExecuteConfigValidateAsync(CommandRequest request)
{
	ResolvedOptions resolved;
	try
	{
		resolved = OptionResolver.Resolve(request.Host, request.Token, request.Organization, json: false);
	}
	catch (Exception ex)
	{
		Console.Error.WriteLine(ex.Message);
		return ExitCodeGeneralFailure;
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
	return ExitCodeSuccess;
}

static int ClassifyValidationFailure(IReadOnlyCollection<string>? errors)
{
	if (errors is null || errors.Count == 0)
	{
		return ExitCodeGeneralFailure;
	}

	var combined = string.Join("\n", errors).ToLowerInvariant();

	if (combined.Contains("401", StringComparison.Ordinal) ||
		combined.Contains("403", StringComparison.Ordinal) ||
		combined.Contains("unauthorized", StringComparison.Ordinal) ||
		combined.Contains("forbidden", StringComparison.Ordinal))
	{
		return ExitCodeAuthFailure;
	}

	if (combined.Contains("name or service not known", StringComparison.Ordinal) ||
		combined.Contains("no such host", StringComparison.Ordinal) ||
		combined.Contains("connection refused", StringComparison.Ordinal) ||
		combined.Contains("timed out", StringComparison.Ordinal) ||
		combined.Contains("network", StringComparison.Ordinal) ||
		combined.Contains("dns", StringComparison.Ordinal))
	{
		return ExitCodeNetworkFailure;
	}

	return ExitCodeGeneralFailure;
}

static int ExecuteConfigInit()
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
		return 1;
	}

	config.Host = host.Trim();
	config.Token = string.IsNullOrWhiteSpace(token) ? null : token.Trim();
	config.Organization = string.IsNullOrWhiteSpace(organization) ? null : organization.Trim();

	UserConfigStore.Save(config);
	Console.WriteLine($"Config saved to: {UserConfigStore.GetConfigPath()}");
	return 0;
}

static string? Prompt(string label, string? currentValue)
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

static int ExecuteConfigSet(CommandRequest request)
{
	if (string.IsNullOrWhiteSpace(request.Host) &&
		string.IsNullOrWhiteSpace(request.Token) &&
		string.IsNullOrWhiteSpace(request.Organization))
	{
		Console.Error.WriteLine("config set requires at least one of --host, --token, or --org.");
		return 1;
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
	return 0;
}

static int ExecuteConfigShow()
{
	var config = UserConfigStore.Load();
	Console.WriteLine($"Config file: {UserConfigStore.GetConfigPath()}");
	Console.WriteLine($"host: {config.Host ?? "<empty>"}");
	Console.WriteLine($"token: {MaskToken(config.Token)}");
	Console.WriteLine($"organization: {config.Organization ?? "<empty>"}");
	return 0;
}

static int ExecuteConfigClear()
{
	var removed = UserConfigStore.Clear();
	Console.WriteLine(removed ? "Config cleared." : "Config file not found.");
	return 0;
}

static string MaskToken(string? token)
{
	if (string.IsNullOrWhiteSpace(token))
	{
		return "<empty>";
	}

	var t = token.Trim();
	if (t.Length <= 8)
	{
		return "***";
	}

	return $"{t[..4]}...{t[^4..]}";
}

enum CommandKind
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

sealed class CliOptions
{
	public string? Host { get; set; }
	public string? Token { get; set; }
	public string? Organization { get; set; }
	public string? Name { get; set; }
	public int PageIndex { get; set; } = 0;
	public int PageSize { get; set; } = 10;
	public bool FetchAll { get; set; }
	public bool Json { get; set; }
}

sealed record CommandRequest(CommandKind Kind, Guid? ProjectId, Guid? EnvId, CliOptions Options)
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

readonly record struct ParseOutcome(bool Success, CommandRequest? Value, string? Error)
{
	public static ParseOutcome Ok(CommandRequest value) => new(true, value, null);

	public static ParseOutcome Fail(string error) => new(false, null, error);
}
