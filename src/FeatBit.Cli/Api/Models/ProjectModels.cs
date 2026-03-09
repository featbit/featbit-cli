using System.Text.Json.Serialization;

namespace FeatBit.Cli.Api.Models;

public sealed class ProjectWithEnvs
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("key")]
    public string? Key { get; init; }

    [JsonPropertyName("environments")]
    public List<EnvironmentInfo>? Environments { get; init; }
}

public sealed class EnvironmentInfo
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("key")]
    public string? Key { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }
}
