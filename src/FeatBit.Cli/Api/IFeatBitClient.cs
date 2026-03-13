using FeatBit.Cli.Api.Models;

namespace FeatBit.Cli.Api;

public interface IFeatBitClient
{
    Task<ApiResponse<List<ProjectWithEnvs>>> GetProjectsAsync(CancellationToken cancellationToken);

    Task<ApiResponse<ProjectWithEnvs>> GetProjectAsync(Guid projectId, CancellationToken cancellationToken);

    Task<ApiResponse<PagedResult<FeatureFlagVm>>> GetFeatureFlagsAsync(
        Guid envId,
        FeatureFlagQuery query,
        CancellationToken cancellationToken);

    Task<WriteResult> ToggleFeatureFlagAsync(Guid envId, string key, bool status, CancellationToken cancellationToken);

    Task<WriteResult> ArchiveFeatureFlagAsync(Guid envId, string key, CancellationToken cancellationToken);

    Task<WriteResult> CreateFeatureFlagAsync(
        Guid envId, string name, string key, string? description, CancellationToken cancellationToken);

    Task<WriteResult> UpdateFeatureFlagRolloutAsync(
        Guid envId, string key, string rolloutAssignments, string? dispatchKey, CancellationToken cancellationToken);

    Task<WriteResult> EvaluateFeatureFlagsAsync(
        string evalHost,
        string envSecret,
        string userKeyId,
        string? userName,
        string? customProperties,
        string? flagKeys,
        string? tags,
        string? tagFilterMode,
        CancellationToken cancellationToken);
}
