using FeatBit.Cli.Api;
using FeatBit.Cli.Commands;
using FeatBit.Cli.Configuration;

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
	Console.WriteLine("featbit project list [--host <url>] [--token <token>] [--org <orgId>] [--json]");
	Console.WriteLine("featbit project get <projectId> [--host <url>] [--token <token>] [--org <orgId>] [--json]");
	Console.WriteLine("featbit flag list <envId> [--name <name>] [--page-index <n>] [--page-size <n>] [--all] [--host <url>] [--token <token>] [--org <orgId>] [--json]");
}

enum CommandKind
{
	ProjectList,
	ProjectGet,
	FlagList
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
