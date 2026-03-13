using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using FeatBit.Cli.Api.Models;
using FeatBit.Cli.Serialization;

namespace FeatBit.Cli.Api;

public sealed class FeatBitClient : IFeatBitClient
{
    private readonly HttpClient _httpClient;
    private readonly string _token;
    private readonly string? _organization;

    public FeatBitClient(HttpClient httpClient, string host, string token, string? organization)
    {
        _httpClient = httpClient;
        _token = token;
        _organization = organization;

        if (!Uri.TryCreate(host, UriKind.Absolute, out var baseUri))
        {
            throw new ArgumentException("Invalid host URL.", nameof(host));
        }

        _httpClient.BaseAddress = baseUri;
        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", NormalizeAuthorizationValue());

        if (!string.IsNullOrWhiteSpace(_organization))
        {
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Organization", _organization);
        }
    }

    public Task<ApiResponse<List<ProjectWithEnvs>>> GetProjectsAsync(CancellationToken cancellationToken)
    {
        return SendGetAsync("/api/v1/projects", FeatBitJsonContext.Default.ApiResponseListProjectWithEnvs, cancellationToken);
    }

    public Task<ApiResponse<ProjectWithEnvs>> GetProjectAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var path = $"/api/v1/projects/{projectId}";
        return SendGetAsync(path, FeatBitJsonContext.Default.ApiResponseProjectWithEnvs, cancellationToken);
    }

    public Task<ApiResponse<PagedResult<FeatureFlagVm>>> GetFeatureFlagsAsync(
        Guid envId,
        FeatureFlagQuery query,
        CancellationToken cancellationToken)
    {
        var queryParts = new List<string>(capacity: 3)
        {
            $"PageIndex={query.PageIndex}",
            $"PageSize={query.PageSize}"
        };

        if (!string.IsNullOrWhiteSpace(query.Name))
        {
            queryParts.Add($"Name={Uri.EscapeDataString(query.Name)}");
        }

        var path = $"/api/v1/envs/{envId}/feature-flags?{string.Join('&', queryParts)}";

        return SendGetAsync(path, FeatBitJsonContext.Default.ApiResponsePagedResultFeatureFlagVm, cancellationToken);
    }

    public Task<WriteResult> ToggleFeatureFlagAsync(Guid envId, string key, bool status, CancellationToken cancellationToken)
        => SendWriteAsync(
            HttpMethod.Put,
            $"/api/v1/envs/{envId}/feature-flags/{Uri.EscapeDataString(key)}/toggle/{status.ToString().ToLower()}",
            body: null,
            cancellationToken);

    public Task<WriteResult> ArchiveFeatureFlagAsync(Guid envId, string key, CancellationToken cancellationToken)
        => SendWriteAsync(
            HttpMethod.Put,
            $"/api/v1/envs/{envId}/feature-flags/{Uri.EscapeDataString(key)}/archive",
            body: null,
            cancellationToken);

    public Task<WriteResult> CreateFeatureFlagAsync(
        Guid envId, string name, string key, string? description, CancellationToken cancellationToken)
    {
        // Always creates a boolean flag with two default variations (true / false).
        var trueVarId  = Guid.NewGuid().ToString();
        var falseVarId = Guid.NewGuid().ToString();

        var sb = new StringBuilder();
        sb.Append("{\"envId\":\"");
        sb.Append(envId);
        sb.Append("\",\"name\":");
        AppendJsonString(sb, name);
        sb.Append(",\"key\":");
        AppendJsonString(sb, key);
        sb.Append(",\"isEnabled\":false");
        sb.Append(",\"variationType\":\"boolean\"");
        sb.Append(",\"variations\":[");
        sb.Append("{\"id\":\""); sb.Append(trueVarId);  sb.Append("\",\"value\":\"true\",\"name\":\"True\"}");
        sb.Append(",{\"id\":\""); sb.Append(falseVarId); sb.Append("\",\"value\":\"false\",\"name\":\"False\"}");
        sb.Append(']');
        sb.Append(",\"enabledVariationId\":\"");  sb.Append(trueVarId);  sb.Append('"');
        sb.Append(",\"disabledVariationId\":\""); sb.Append(falseVarId); sb.Append('"');
        if (description is not null)
        {
            sb.Append(",\"description\":");
            AppendJsonString(sb, description);
        }
        sb.Append('}');

        return SendWriteAsync(
            HttpMethod.Post,
            $"/api/v1/envs/{envId}/feature-flags",
            sb.ToString(),
            cancellationToken);
    }

    public Task<WriteResult> UpdateFeatureFlagRolloutAsync(
        Guid envId, string key, string rolloutAssignments, string? dispatchKey, CancellationToken cancellationToken)
    {
        using var doc = JsonDocument.Parse(rolloutAssignments);
        var assignments = doc.RootElement.EnumerateArray()
            .Select(el => (
                variationId: el.GetProperty("variationId").GetString()!,
                percentage:  el.GetProperty("percentage").GetDouble()))
            .ToArray();

        var total = assignments.Sum(a => a.percentage);
        if (Math.Abs(total - 100.0) > 0.01)
            return Task.FromResult(new WriteResult
            {
                Success = false,
                Error = $"Percentages must sum to 100, but got {total}."
            });

        double cursor = 0.0;
        var variationsSb = new StringBuilder();
        variationsSb.Append('[');
        for (var i = 0; i < assignments.Length; i++)
        {
            var (variationId, percentage) = assignments[i];
            var start = Math.Round(cursor, 4);
            cursor += percentage / 100.0;
            var end = Math.Round(cursor, 4);

            if (i > 0) variationsSb.Append(',');
            variationsSb.Append("{\"id\":");
            AppendJsonString(variationsSb, variationId);
            variationsSb.Append(",\"rollout\":[");
            variationsSb.Append(start.ToString("G", System.Globalization.CultureInfo.InvariantCulture));
            variationsSb.Append(',');
            variationsSb.Append(end.ToString("G", System.Globalization.CultureInfo.InvariantCulture));
            variationsSb.Append("],\"exptRollout\":1.0}");
        }
        variationsSb.Append(']');

        var sb = new StringBuilder();
        sb.Append("[{\"op\":\"replace\",\"path\":\"/fallthrough\",\"value\":{\"dispatchKey\":");
        if (dispatchKey is null)
            sb.Append("null");
        else
            AppendJsonString(sb, dispatchKey);
        sb.Append(",\"includedInExpt\":false,\"variations\":");
        sb.Append(variationsSb);
        sb.Append("}}]");

        return SendWriteAsync(
            HttpMethod.Patch,
            $"/api/v1/envs/{envId}/feature-flags/{Uri.EscapeDataString(key)}",
            sb.ToString(),
            cancellationToken);
    }

    public async Task<WriteResult> EvaluateFeatureFlagsAsync(
        string evalHost,
        string envSecret,
        string userKeyId,
        string? userName,
        string? customProperties,
        string? flagKeys,
        string? tags,
        string? tagFilterMode,
        CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();
        sb.Append("{\"user\":{\"keyId\":");
        AppendJsonString(sb, userKeyId);
        if (!string.IsNullOrWhiteSpace(userName))
        {
            sb.Append(",\"name\":");
            AppendJsonString(sb, userName);
        }
        if (!string.IsNullOrWhiteSpace(customProperties))
        {
            // Embed the raw custom-properties JSON array as-is (validated below)
            try
            {
                using var propsDoc = JsonDocument.Parse(customProperties);
                if (propsDoc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    sb.Append(",\"customizedProperties\":");
                    sb.Append(customProperties);
                }
            }
            catch { /* ignore invalid JSON */ }
        }
        sb.Append('}');

        var hasKeys = !string.IsNullOrWhiteSpace(flagKeys);
        var hasTags = !string.IsNullOrWhiteSpace(tags);
        if (hasKeys || hasTags)
        {
            sb.Append(",\"filter\":{");
            var needsComma = false;
            if (hasKeys)
            {
                sb.Append("\"keys\":[");
                var parts = flagKeys!.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                for (var i = 0; i < parts.Length; i++)
                {
                    if (i > 0) sb.Append(',');
                    AppendJsonString(sb, parts[i]);
                }
                sb.Append(']');
                needsComma = true;
            }
            if (hasTags)
            {
                if (needsComma) sb.Append(',');
                sb.Append("\"tags\":[");
                var parts = tags!.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                for (var i = 0; i < parts.Length; i++)
                {
                    if (i > 0) sb.Append(',');
                    AppendJsonString(sb, parts[i]);
                }
                sb.Append("],\"tagFilterMode\":");
                AppendJsonString(sb, string.IsNullOrWhiteSpace(tagFilterMode) ? "and" : tagFilterMode);
            }
            sb.Append('}');
        }
        sb.Append('}');

        var fullUrl = evalHost.TrimEnd('/') + "/api/public/featureflag/evaluate";

        HttpResponseMessage response;
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, fullUrl);
            request.Headers.TryAddWithoutValidation("Authorization", envSecret);
            request.Content = new StringContent(sb.ToString(), Encoding.UTF8, "application/json");
            response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            return new WriteResult { Success = false, Error = $"Network error: {ex.Message}" };
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            return new WriteResult { Success = false, Error = $"Network error: request timed out. {ex.Message}" };
        }

        using (response)
        {
            var rawBody = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
                return new WriteResult { Success = false, Error = $"HTTP {(int)response.StatusCode} {response.StatusCode}: {rawBody}", RawJson = rawBody };
            return new WriteResult { Success = true, RawJson = rawBody };
        }
    }

    private async Task<WriteResult> SendWriteAsync(
        HttpMethod method, string path, string? body, CancellationToken cancellationToken)
    {
        HttpResponseMessage response;
        try
        {
            using var request = new HttpRequestMessage(method, path);
            if (body is not null)
                request.Content = new StringContent(body, Encoding.UTF8, "application/json");
            response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            return new WriteResult { Success = false, Error = $"Network error: {ex.Message}" };
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            return new WriteResult { Success = false, Error = $"Network error: request timed out. {ex.Message}" };
        }

        using (response)
        {
            var rawBody = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
                return new WriteResult
                {
                    Success = false,
                    RawJson = rawBody,
                    Error = $"HTTP {(int)response.StatusCode} {response.StatusCode}: " +
                            (string.IsNullOrWhiteSpace(rawBody) ? "No error body returned." : rawBody)
                };
            return new WriteResult { Success = true, RawJson = rawBody };
        }
    }

    private async Task<ApiResponse<T>> SendGetAsync<T>(
        string path,
        JsonTypeInfo<ApiResponse<T>> responseTypeInfo,
        CancellationToken cancellationToken)
    {
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync(path, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Errors = [$"Network error: {ex.Message}"]
            };
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Errors = [$"Network error: request timed out. {ex.Message}"]
            };
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                return new ApiResponse<T>
                {
                    Success = false,
                    Errors =
                    [
                        $"HTTP {(int)response.StatusCode} {response.StatusCode}",
                        string.IsNullOrWhiteSpace(body) ? "No error body returned." : body
                    ]
                };
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var payload = await JsonSerializer.DeserializeAsync(stream, responseTypeInfo, cancellationToken);
            if (payload is null)
            {
                return new ApiResponse<T>
                {
                    Success = false,
                    Errors = ["Empty or invalid JSON response."]
                };
            }

            return payload;
        }
    }

    /// <summary>Appends a JSON-encoded string value (with surrounding quotes) to <paramref name="sb"/>.</summary>
    private static void AppendJsonString(StringBuilder sb, string value)
    {
        sb.Append('"');
        foreach (var c in value)
        {
            switch (c)
            {
                case '"':  sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\n': sb.Append("\\n");  break;
                case '\r': sb.Append("\\r");  break;
                case '\t': sb.Append("\\t");  break;
                default:
                    if (c < 0x20)
                        sb.Append($"\\u{(int)c:X4}");
                    else
                        sb.Append(c);
                    break;
            }
        }
        sb.Append('"');
    }

    private string NormalizeAuthorizationValue()
    {
        var value = _token.Trim();
        if (value.StartsWith("api-", StringComparison.OrdinalIgnoreCase))
        {
            return value;
        }

        return $"api-{value}";
    }
}
