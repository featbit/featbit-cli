using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using FeatBit.Cli.Api;
using FeatBit.Cli.Api.Models;
using FeatBit.Cli.Output;
using FeatBit.Cli.Serialization;

namespace FeatBit.Cli.Commands;

public static class CommandExecutors
{
    public static async Task<int> ProjectListAsync(
        IFeatBitClient client,
        bool asJson,
        TextWriter stdout,
        TextWriter stderr,
        CancellationToken cancellationToken)
    {
        var response = await client.GetProjectsAsync(cancellationToken);
        if (!TryGetData(response, stderr, out var projects))
        {
            return 1;
        }

        if (asJson)
        {
            await WriteJsonAsync(stdout, response, FeatBitJsonContext.Default.ApiResponseListProjectWithEnvs);
            return 0;
        }

        var rows = projects
            .Select(p => (IReadOnlyList<string>)
            [
                p.Id.ToString(),
                p.Name ?? string.Empty,
                p.Key ?? string.Empty,
                (p.Environments?.Count ?? 0).ToString()
            ])
            .ToList();

        TablePrinter.Print(stdout, ["Id", "Name", "Key", "EnvCount"], rows);
        return 0;
    }

    public static async Task<int> ProjectGetAsync(
        IFeatBitClient client,
        Guid projectId,
        bool asJson,
        TextWriter stdout,
        TextWriter stderr,
        CancellationToken cancellationToken)
    {
        var response = await client.GetProjectAsync(projectId, cancellationToken);
        if (!TryGetData(response, stderr, out var project))
        {
            return 1;
        }

        if (asJson)
        {
            await WriteJsonAsync(stdout, response, FeatBitJsonContext.Default.ApiResponseProjectWithEnvs);
            return 0;
        }

        await stdout.WriteLineAsync($"Project: {project.Name} ({project.Key})");
        await stdout.WriteLineAsync($"Id: {project.Id}");
        await stdout.WriteLineAsync();

        var envs = project.Environments ?? [];
        if (envs.Count == 0)
        {
            await stdout.WriteLineAsync("No environments found.");
            return 0;
        }

        var rows = envs
            .Select(e => (IReadOnlyList<string>)
            [
                e.Id.ToString(),
                e.Name ?? string.Empty,
                e.Key ?? string.Empty,
                e.Description ?? string.Empty
            ])
            .ToList();

        TablePrinter.Print(stdout, ["EnvId", "Name", "Key", "Description"], rows);
        return 0;
    }

    public static async Task<int> FlagListAsync(
        IFeatBitClient client,
        Guid envId,
        string? name,
        int pageIndex,
        int pageSize,
        bool fetchAll,
        bool asJson,
        TextWriter stdout,
        TextWriter stderr,
        CancellationToken cancellationToken)
    {
        ApiResponse<PagedResult<FeatureFlagVm>> response;

        if (!fetchAll)
        {
            response = await client.GetFeatureFlagsAsync(envId, new FeatureFlagQuery(name, pageIndex, pageSize), cancellationToken);
        }
        else
        {
            response = await GetAllFlagsAsync(client, envId, name, pageIndex, pageSize, cancellationToken);
        }

        if (!TryGetData(response, stderr, out var pagedFlags))
        {
            return 1;
        }

        if (asJson)
        {
            await WriteJsonAsync(stdout, response, FeatBitJsonContext.Default.ApiResponsePagedResultFeatureFlagVm);
            return 0;
        }

        var items = pagedFlags.Items ?? [];
        var rows = items
            .Select(f => (IReadOnlyList<string>)
            [
                f.Id.ToString(),
                f.Key ?? string.Empty,
                f.Name ?? string.Empty,
                f.IsEnabled ? "on" : "off",
                f.VariationType ?? string.Empty,
                f.Tags is { Count: > 0 } ? string.Join(',', f.Tags) : string.Empty
            ])
            .ToList();

        TablePrinter.Print(stdout, ["Id", "Key", "Name", "Enabled", "Type", "Tags"], rows);
        await stdout.WriteLineAsync($"TotalCount: {pagedFlags.TotalCount}");
        return 0;
    }

    public static async Task<int> FlagToggleAsync(
        IFeatBitClient client,
        Guid envId,
        string key,
        bool status,
        bool dryRun,
        bool asJson,
        TextWriter stdout,
        TextWriter stderr,
        CancellationToken cancellationToken)
    {
        if (dryRun)
        {
            await stdout.WriteLineAsync($"Would {(status ? "enable" : "disable")} feature flag '{key}'.");
            await stdout.WriteLineAsync("No changes made.");
            return 0;
        }

        var result = await client.ToggleFeatureFlagAsync(envId, key, status, cancellationToken);
        if (!result.Success)
        {
            await stderr.WriteLineAsync(result.Error ?? "Unknown error.");
            return 1;
        }

        if (asJson)
        {
            await stdout.WriteLineAsync(result.RawJson);
            return 0;
        }

        await stdout.WriteLineAsync($"Feature flag '{key}' is now {(status ? "enabled" : "disabled")}.");
        return 0;
    }

    public static async Task<int> FlagArchiveAsync(
        IFeatBitClient client,
        Guid envId,
        string key,
        bool dryRun,
        bool asJson,
        TextWriter stdout,
        TextWriter stderr,
        CancellationToken cancellationToken)
    {
        if (dryRun)
        {
            await stdout.WriteLineAsync($"Would archive feature flag '{key}'.");
            await stdout.WriteLineAsync("No changes made.");
            return 0;
        }

        var result = await client.ArchiveFeatureFlagAsync(envId, key, cancellationToken);
        if (!result.Success)
        {
            await stderr.WriteLineAsync(result.Error ?? "Unknown error.");
            return 1;
        }

        if (asJson)
        {
            await stdout.WriteLineAsync(result.RawJson);
            return 0;
        }

        await stdout.WriteLineAsync($"Feature flag '{key}' has been archived.");
        return 0;
    }

    public static async Task<int> FlagCreateAsync(
        IFeatBitClient client,
        Guid envId,
        string name,
        string key,
        string? description,
        bool asJson,
        TextWriter stdout,
        TextWriter stderr,
        CancellationToken cancellationToken)
    {
        var result = await client.CreateFeatureFlagAsync(envId, name, key, description, cancellationToken);
        if (!result.Success)
        {
            await stderr.WriteLineAsync(result.Error ?? "Unknown error.");
            return 1;
        }

        if (asJson)
        {
            await stdout.WriteLineAsync(result.RawJson);
            return 0;
        }

        await stdout.WriteLineAsync($"Feature flag '{name}' (key: {key}) created successfully.");
        return 0;
    }

    public static async Task<int> FlagSetRolloutAsync(
        IFeatBitClient client,
        Guid envId,
        string key,
        string rolloutAssignments,
        string? dispatchKey,
        bool asJson,
        TextWriter stdout,
        TextWriter stderr,
        CancellationToken cancellationToken)
    {
        WriteResult result;
        try
        {
            result = await client.UpdateFeatureFlagRolloutAsync(envId, key, rolloutAssignments, dispatchKey, cancellationToken);
        }
        catch (Exception ex)
        {
            await stderr.WriteLineAsync($"Invalid rollout JSON: {ex.Message}");
            return 1;
        }

        if (!result.Success)
        {
            await stderr.WriteLineAsync(result.Error ?? "Unknown error.");
            return 1;
        }

        if (asJson)
        {
            await stdout.WriteLineAsync(result.RawJson);
            return 0;
        }

        await stdout.WriteLineAsync($"Rollout for feature flag '{key}' updated successfully.");
        return 0;
    }

    public static async Task<int> FlagEvaluateAsync(
        IFeatBitClient client,
        string evalHost,
        string envSecret,
        string userKeyId,
        string? userName,
        string? customProperties,
        string? flagKeys,
        string? tags,
        string? tagFilterMode,
        bool asJson,
        TextWriter stdout,
        TextWriter stderr,
        CancellationToken cancellationToken)
    {
        var result = await client.EvaluateFeatureFlagsAsync(
            evalHost, envSecret, userKeyId, userName, customProperties,
            flagKeys, tags, tagFilterMode, cancellationToken);

        if (!result.Success)
        {
            await stderr.WriteLineAsync(result.Error ?? "Unknown error.");
            return 1;
        }

        if (asJson)
        {
            await stdout.WriteLineAsync(result.RawJson);
            return 0;
        }

        // Parse raw JSON and render a table — handle both array and ApiResponse-wrapped shapes.
        try
        {
            using var doc = JsonDocument.Parse(result.RawJson!);
            var root = doc.RootElement;

            JsonElement items;
            if (root.ValueKind == JsonValueKind.Array)
            {
                items = root;
            }
            else if (root.ValueKind == JsonValueKind.Object
                     && root.TryGetProperty("data", out var dataEl)
                     && dataEl.ValueKind == JsonValueKind.Array)
            {
                items = dataEl;
            }
            else
            {
                await stdout.WriteLineAsync(result.RawJson);
                return 0;
            }

            var rows = items.EnumerateArray()
                .Select(el => (IReadOnlyList<string>)
                [
                    TryGetString(el, "key"),
                    TryGetString(el, "variation"),
                    TryGetString(el, "matchReason")
                ])
                .ToList();

            if (rows.Count == 0)
            {
                await stdout.WriteLineAsync("No flags evaluated.");
                return 0;
            }

            TablePrinter.Print(stdout, ["Key", "Variation", "MatchReason"], rows);
        }
        catch
        {
            await stdout.WriteLineAsync(result.RawJson);
        }

        return 0;
    }

    private static string TryGetString(JsonElement el, string property)
        => el.TryGetProperty(property, out var val) ? val.GetString() ?? string.Empty : string.Empty;

    private static async Task<ApiResponse<PagedResult<FeatureFlagVm>>> GetAllFlagsAsync(
        IFeatBitClient client,
        Guid envId,
        string? name,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var cursor = pageIndex;
        var items = new List<FeatureFlagVm>();
        long totalCount = 0;

        while (true)
        {
            var response = await client.GetFeatureFlagsAsync(envId, new FeatureFlagQuery(name, cursor, pageSize), cancellationToken);
            if (!response.Success || response.Data is null)
            {
                return response;
            }

            totalCount = response.Data.TotalCount;
            var pageItems = response.Data.Items ?? [];
            items.AddRange(pageItems);

            if (pageItems.Count == 0)
            {
                break;
            }

            if (totalCount > 0 && items.Count >= totalCount)
            {
                break;
            }

            cursor++;
        }

        return new ApiResponse<PagedResult<FeatureFlagVm>>
        {
            Success = true,
            Data = new PagedResult<FeatureFlagVm>
            {
                TotalCount = totalCount,
                Items = items
            }
        };
    }

    private static async Task WriteJsonAsync<T>(
        TextWriter stdout,
        ApiResponse<T> response,
        JsonTypeInfo<ApiResponse<T>> typeInfo)
    {
        var json = JsonSerializer.Serialize(response, typeInfo);
        await stdout.WriteLineAsync(json);
    }

    private static bool TryGetData<T>(ApiResponse<T> response, TextWriter stderr, out T data)
    {
        if (response.Success && response.Data is not null)
        {
            data = response.Data;
            return true;
        }

        WriteErrors(response.Errors, stderr);
        data = default!;
        return false;
    }

    private static void WriteErrors(List<string>? errors, TextWriter stderr)
    {
        if (errors is null || errors.Count == 0)
        {
            stderr.WriteLine("Request failed with unknown error.");
            return;
        }

        foreach (var error in errors)
        {
            stderr.WriteLine(error);
        }
    }
}
