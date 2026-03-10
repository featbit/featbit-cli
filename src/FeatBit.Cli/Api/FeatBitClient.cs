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
