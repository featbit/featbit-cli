using System.Text.Json;
using System.Text.Json.Serialization;

namespace FeatBit.Cli.Api.Models;

public sealed class AuditLogVm
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("refId")]
    public string? RefId { get; init; }

    [JsonPropertyName("refType")]
    public string? RefType { get; init; }

    [JsonPropertyName("operation")]
    public string? Operation { get; init; }

    [JsonPropertyName("dataChange")]
    public DataChange? DataChange { get; init; }

    [JsonPropertyName("comment")]
    public string? Comment { get; init; }

    [JsonPropertyName("creatorId")]
    public Guid CreatorId { get; init; }

    [JsonPropertyName("creatorName")]
    public string? CreatorName { get; init; }

    [JsonPropertyName("creatorEmail")]
    public string? CreatorEmail { get; init; }

    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("instructions")]
    public List<AuditInstruction>? Instructions { get; init; }
}

public sealed class DataChange
{
    [JsonPropertyName("previous")]
    public string? Previous { get; init; }

    [JsonPropertyName("current")]
    public string? Current { get; init; }
}

public sealed class AuditInstruction
{
    [JsonPropertyName("kind")]
    public string? Kind { get; init; }

    [JsonPropertyName("value")]
    public JsonElement Value { get; init; }
}
