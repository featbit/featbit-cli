using System.Text.Json.Serialization;
using FeatBit.Cli.Api.Models;
using FeatBit.Cli.Configuration;

namespace FeatBit.Cli.Serialization;

[JsonSerializable(typeof(ApiResponse<List<ProjectWithEnvs>>))]
[JsonSerializable(typeof(ApiResponse<ProjectWithEnvs>))]
[JsonSerializable(typeof(ApiResponse<ProjectFeatureFlags>))]
[JsonSerializable(typeof(ApiResponse<FeatureFlagVm>))]
[JsonSerializable(typeof(ApiResponse<PagedResult<FeatureFlagVm>>))]
[JsonSerializable(typeof(ApiResponse<PagedResult<AuditLogVm>>))]
[JsonSerializable(typeof(PagedResult<FeatureFlagVm>))]
[JsonSerializable(typeof(PagedResult<AuditLogVm>))]
[JsonSerializable(typeof(List<ProjectWithEnvs>))]
[JsonSerializable(typeof(List<FeatureFlagVm>))]
[JsonSerializable(typeof(List<ProjectEnvironmentFeatureFlags>))]
[JsonSerializable(typeof(List<AuditLogVm>))]
[JsonSerializable(typeof(UserConfig))]
internal sealed partial class FeatBitJsonContext : JsonSerializerContext
{
}
