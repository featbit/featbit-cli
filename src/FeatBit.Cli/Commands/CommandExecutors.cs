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
        string? tags,
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
            response = await client.GetFeatureFlagsAsync(envId, new FeatureFlagQuery(name, tags, pageIndex, pageSize), cancellationToken);
        }
        else
        {
            response = await GetAllFlagsAsync(client, envId, name, tags, pageIndex, pageSize, cancellationToken);
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

    public static async Task<int> ProjectFlagsAsync(
        IFeatBitClient client,
        Guid projectId,
        string? name,
        string? tags,
        int pageIndex,
        int pageSize,
        bool fetchAll,
        bool asJson,
        TextWriter stdout,
        TextWriter stderr,
        CancellationToken cancellationToken)
    {
        var projectResponse = await client.GetProjectAsync(projectId, cancellationToken);
        if (!TryGetData(projectResponse, stderr, out var project))
        {
            return 1;
        }

        var envs = project.Environments ?? [];
        var envResults = new List<ProjectEnvironmentFeatureFlags>(envs.Count);
        foreach (var env in envs)
        {
            var flagsResponse = fetchAll
                ? await GetAllFlagsAsync(client, env.Id, name, tags, pageIndex, pageSize, cancellationToken)
                : await client.GetFeatureFlagsAsync(env.Id, new FeatureFlagQuery(name, tags, pageIndex, pageSize), cancellationToken);

            if (!TryGetData(flagsResponse, stderr, out var pagedFlags))
            {
                return 1;
            }

            envResults.Add(new ProjectEnvironmentFeatureFlags
            {
                EnvId = env.Id,
                EnvName = env.Name,
                EnvKey = env.Key,
                TotalCount = pagedFlags.TotalCount,
                Items = pagedFlags.Items ?? []
            });
        }

        var response = new ApiResponse<ProjectFeatureFlags>
        {
            Success = true,
            Data = new ProjectFeatureFlags
            {
                ProjectId = project.Id,
                ProjectName = project.Name,
                ProjectKey = project.Key,
                Environments = envResults
            }
        };

        if (asJson)
        {
            await WriteJsonAsync(stdout, response, FeatBitJsonContext.Default.ApiResponseProjectFeatureFlags);
            return 0;
        }

        await stdout.WriteLineAsync($"Project: {project.Name} ({project.Key})");
        await stdout.WriteLineAsync($"Id: {project.Id}");
        await stdout.WriteLineAsync();

        var rows = envResults
            .SelectMany(env => (env.Items ?? []).Select(flag => (IReadOnlyList<string>)
            [
                env.EnvId.ToString(),
                env.EnvKey ?? string.Empty,
                flag.Id.ToString(),
                flag.Key ?? string.Empty,
                flag.Name ?? string.Empty,
                flag.IsEnabled ? "on" : "off",
                flag.VariationType ?? string.Empty,
                flag.Tags is { Count: > 0 } ? string.Join(',', flag.Tags) : string.Empty
            ]))
            .ToList();

        if (rows.Count == 0)
        {
            await stdout.WriteLineAsync("No feature flags found.");
            return 0;
        }

        TablePrinter.Print(stdout, ["EnvId", "EnvKey", "FlagId", "Key", "Name", "Enabled", "Type", "Tags"], rows);
        await stdout.WriteLineAsync($"EnvironmentCount: {envResults.Count}");
        await stdout.WriteLineAsync($"TotalCount: {envResults.Sum(env => env.TotalCount)}");
        return 0;
    }

    public static async Task<int> FlagAuditLogsAsync(
        IFeatBitClient client,
        Guid envId,
        Guid? flagId,
        string? flagKey,
        string? query,
        Guid? creatorId,
        long? from,
        long? to,
        bool crossEnvironment,
        int pageIndex,
        int pageSize,
        bool fetchAll,
        bool asJson,
        TextWriter stdout,
        TextWriter stderr,
        CancellationToken cancellationToken)
    {
        var refId = flagId?.ToString();
        if (string.IsNullOrWhiteSpace(refId) && !string.IsNullOrWhiteSpace(flagKey))
        {
            var flagResponse = await client.GetFeatureFlagAsync(envId, flagKey, cancellationToken);
            if (!TryGetData(flagResponse, stderr, out var flag))
            {
                return 1;
            }

            refId = flag.Id.ToString();
        }

        var auditQuery = new AuditLogQuery(
            query,
            creatorId,
            refId,
            "FeatureFlag",
            from,
            to,
            crossEnvironment,
            pageIndex,
            pageSize);

        var response = fetchAll
            ? await GetAllAuditLogsAsync(client, envId, auditQuery, cancellationToken)
            : await client.GetAuditLogsAsync(envId, auditQuery, cancellationToken);

        if (!TryGetData(response, stderr, out var pagedLogs))
        {
            return 1;
        }

        if (asJson)
        {
            await WriteJsonAsync(stdout, response, FeatBitJsonContext.Default.ApiResponsePagedResultAuditLogVm);
            return 0;
        }

        var items = pagedLogs.Items ?? [];
        var rows = items
            .Select(log => (IReadOnlyList<string>)
            [
                log.CreatedAt == default ? string.Empty : log.CreatedAt.ToString("u"),
                log.Operation ?? string.Empty,
                log.RefType ?? string.Empty,
                log.RefId ?? string.Empty,
                log.CreatorName ?? log.CreatorEmail ?? log.CreatorId.ToString(),
                log.Comment ?? string.Empty
            ])
            .ToList();

        TablePrinter.Print(stdout, ["CreatedAt", "Operation", "RefType", "RefId", "Creator", "Comment"], rows);
        await stdout.WriteLineAsync($"TotalCount: {pagedLogs.TotalCount}");
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
        string? tags,
        bool asJson,
        TextWriter stdout,
        TextWriter stderr,
        CancellationToken cancellationToken)
    {
        var result = await client.CreateFeatureFlagAsync(envId, name, key, description, tags, cancellationToken);
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
        string? tags,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var cursor = pageIndex;
        var items = new List<FeatureFlagVm>();
        long totalCount = 0;

        while (true)
        {
            var response = await client.GetFeatureFlagsAsync(envId, new FeatureFlagQuery(name, tags, cursor, pageSize), cancellationToken);
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

    private static async Task<ApiResponse<PagedResult<AuditLogVm>>> GetAllAuditLogsAsync(
        IFeatBitClient client,
        Guid envId,
        AuditLogQuery query,
        CancellationToken cancellationToken)
    {
        var cursor = query.PageIndex;
        var items = new List<AuditLogVm>();
        long totalCount = 0;

        while (true)
        {
            var response = await client.GetAuditLogsAsync(
                envId,
                query with { PageIndex = cursor },
                cancellationToken);

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

        return new ApiResponse<PagedResult<AuditLogVm>>
        {
            Success = true,
            Data = new PagedResult<AuditLogVm>
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
