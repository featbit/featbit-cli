using System.Text.Json.Serialization;

namespace FeatBit.Cli.Configuration;

public sealed class UserConfig
{
    [JsonPropertyName("host")]
    public string? Host { get; set; }

    [JsonPropertyName("token")]
    public string? Token { get; set; }

    [JsonPropertyName("organization")]
    public string? Organization { get; set; }
}
