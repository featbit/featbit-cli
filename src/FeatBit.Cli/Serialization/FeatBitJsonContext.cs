using System.Text.Json.Serialization;
using FeatBit.Cli.Api.Models;

namespace FeatBit.Cli.Serialization;

[JsonSerializable(typeof(ApiResponse<List<ProjectWithEnvs>>))]
[JsonSerializable(typeof(ApiResponse<ProjectWithEnvs>))]
[JsonSerializable(typeof(ApiResponse<PagedResult<FeatureFlagVm>>))]
[JsonSerializable(typeof(PagedResult<FeatureFlagVm>))]
[JsonSerializable(typeof(List<ProjectWithEnvs>))]
[JsonSerializable(typeof(List<FeatureFlagVm>))]
internal sealed partial class FeatBitJsonContext : JsonSerializerContext
{
}
