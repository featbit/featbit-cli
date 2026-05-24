using System.Text.Json.Serialization;

namespace FeatBit.Cli.Api.Models;

public sealed class FeatureFlagVm
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("key")]
    public string? Key { get; init; }

    [JsonPropertyName("isEnabled")]
    public bool IsEnabled { get; init; }

    [JsonPropertyName("variationType")]
    public string? VariationType { get; init; }

    [JsonPropertyName("variations")]
    public List<Variation>? Variations { get; init; }

    [JsonPropertyName("tags")]
    public List<string>? Tags { get; init; }

    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("updatedAt")]
    public DateTimeOffset UpdatedAt { get; init; }

    [JsonPropertyName("creator")]
    public UserVm? Creator { get; init; }

    [JsonPropertyName("serves")]
    public Serves? Serves { get; init; }

    [JsonPropertyName("lastChange")]
    public LastChangeVm? LastChange { get; init; }
}

public sealed class Variation
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("value")]
    public string? Value { get; init; }
}

public sealed class UserVm
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("email")]
    public string? Email { get; init; }
}

public sealed class Serves
{
    [JsonPropertyName("enabledVariations")]
    public List<string>? EnabledVariations { get; init; }

    [JsonPropertyName("disabledVariation")]
    public string? DisabledVariation { get; init; }
}

public sealed class LastChangeVm
{
    [JsonPropertyName("operator")]
    public UserVm? Operator { get; init; }

    [JsonPropertyName("happenedAt")]
    public DateTimeOffset HappenedAt { get; init; }

    [JsonPropertyName("comment")]
    public string? Comment { get; init; }
}

public sealed class ProjectFeatureFlags
{
    [JsonPropertyName("projectId")]
    public Guid ProjectId { get; init; }

    [JsonPropertyName("projectName")]
    public string? ProjectName { get; init; }

    [JsonPropertyName("projectKey")]
    public string? ProjectKey { get; init; }

    [JsonPropertyName("environments")]
    public List<ProjectEnvironmentFeatureFlags>? Environments { get; init; }
}

public sealed class ProjectEnvironmentFeatureFlags
{
    [JsonPropertyName("envId")]
    public Guid EnvId { get; init; }

    [JsonPropertyName("envName")]
    public string? EnvName { get; init; }

    [JsonPropertyName("envKey")]
    public string? EnvKey { get; init; }

    [JsonPropertyName("totalCount")]
    public long TotalCount { get; init; }

    [JsonPropertyName("items")]
    public List<FeatureFlagVm>? Items { get; init; }
}
