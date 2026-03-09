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
}
