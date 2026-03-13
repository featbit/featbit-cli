using System.Text.Json.Serialization;

namespace FeatBit.Cli.Api.Models;

public sealed class ApiResponse<T>
{
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("errors")]
    public List<string>? Errors { get; init; }

    [JsonPropertyName("data")]
    public T? Data { get; init; }
}

public sealed class PagedResult<T>
{
    [JsonPropertyName("totalCount")]
    public long TotalCount { get; init; }

    [JsonPropertyName("items")]
    public List<T>? Items { get; init; }
}

public sealed class WriteResult
{
    public bool Success { get; init; }

    /// <summary>Raw HTTP response body from the API.</summary>
    public string? RawJson { get; init; }

    public string? Error { get; init; }
}
