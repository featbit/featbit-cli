using System.Text;
using FeatBit.Cli.Api;
using FeatBit.Cli.Api.Models;
using FeatBit.Cli.Commands;
using Xunit;

namespace FeatBit.Cli.Tests;

public sealed class CommandExecutorsTests
{
    [Fact]
    public async Task ProjectListAsync_TableOutput_ShouldContainProjectRow()
    {
        var fake = new FakeClient
        {
            ProjectsResponse = new ApiResponse<List<ProjectWithEnvs>>
            {
                Success = true,
                Data =
                [
                    new ProjectWithEnvs
                    {
                        Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                        Name = "Core",
                        Key = "core",
                        Environments =
                        [
                            new EnvironmentInfo { Id = Guid.NewGuid(), Name = "Prod", Key = "prod" }
                        ]
                    }
                ]
            }
        };

        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var code = await CommandExecutors.ProjectListAsync(fake, false, stdout, stderr, CancellationToken.None);

        Assert.Equal(0, code);
        Assert.Contains("Core", stdout.ToString());
        Assert.Contains("core", stdout.ToString());
        Assert.Equal(string.Empty, stderr.ToString());
    }

    [Fact]
    public async Task FlagListAsync_AllPages_ShouldLoopUntilDone()
    {
        var envId = Guid.NewGuid();

        var fake = new FakeClient
        {
            PagedResponses = new Queue<ApiResponse<PagedResult<FeatureFlagVm>>>(
            [
                new ApiResponse<PagedResult<FeatureFlagVm>>
                {
                    Success = true,
                    Data = new PagedResult<FeatureFlagVm>
                    {
                        TotalCount = 3,
                        Items =
                        [
                            new FeatureFlagVm { Id = Guid.NewGuid(), Key = "k1", Name = "f1", IsEnabled = true },
                            new FeatureFlagVm { Id = Guid.NewGuid(), Key = "k2", Name = "f2", IsEnabled = false }
                        ]
                    }
                },
                new ApiResponse<PagedResult<FeatureFlagVm>>
                {
                    Success = true,
                    Data = new PagedResult<FeatureFlagVm>
                    {
                        TotalCount = 3,
                        Items =
                        [
                            new FeatureFlagVm { Id = Guid.NewGuid(), Key = "k3", Name = "f3", IsEnabled = true }
                        ]
                    }
                }
            ])
        };

        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var code = await CommandExecutors.FlagListAsync(
            fake,
            envId,
            null,
            0,
            2,
            true,
            false,
            stdout,
            stderr,
            CancellationToken.None);

        Assert.Equal(0, code);
        Assert.Equal(2, fake.GetFlagsCallCount);
        Assert.Contains("k1", stdout.ToString());
        Assert.Contains("k3", stdout.ToString());
        Assert.Contains("TotalCount: 3", stdout.ToString());
        Assert.Equal(string.Empty, stderr.ToString());
    }

    [Fact]
    public async Task ProjectGetAsync_JsonOutput_ShouldContainSuccessProperty()
    {
        var projectId = Guid.NewGuid();
        var fake = new FakeClient
        {
            ProjectResponse = new ApiResponse<ProjectWithEnvs>
            {
                Success = true,
                Data = new ProjectWithEnvs { Id = projectId, Name = "Demo", Key = "demo" }
            }
        };

        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var code = await CommandExecutors.ProjectGetAsync(fake, projectId, true, stdout, stderr, CancellationToken.None);

        Assert.Equal(0, code);
        Assert.Contains("\"success\":true", stdout.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(string.Empty, stderr.ToString());
    }

    private sealed class FakeClient : IFeatBitClient
    {
        public ApiResponse<List<ProjectWithEnvs>>? ProjectsResponse { get; init; }
        public ApiResponse<ProjectWithEnvs>? ProjectResponse { get; init; }
        public Queue<ApiResponse<PagedResult<FeatureFlagVm>>> PagedResponses { get; init; } = new();
        public int GetFlagsCallCount { get; private set; }

        public Task<ApiResponse<List<ProjectWithEnvs>>> GetProjectsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(ProjectsResponse ?? new ApiResponse<List<ProjectWithEnvs>> { Success = true, Data = [] });
        }

        public Task<ApiResponse<ProjectWithEnvs>> GetProjectAsync(Guid projectId, CancellationToken cancellationToken)
        {
            return Task.FromResult(ProjectResponse ?? new ApiResponse<ProjectWithEnvs> { Success = false, Errors = ["not found"] });
        }

        public Task<ApiResponse<PagedResult<FeatureFlagVm>>> GetFeatureFlagsAsync(
            Guid envId,
            FeatureFlagQuery query,
            CancellationToken cancellationToken)
        {
            GetFlagsCallCount++;

            if (PagedResponses.Count == 0)
            {
                return Task.FromResult(new ApiResponse<PagedResult<FeatureFlagVm>>
                {
                    Success = true,
                    Data = new PagedResult<FeatureFlagVm> { TotalCount = 0, Items = [] }
                });
            }

            return Task.FromResult(PagedResponses.Dequeue());
        }
    }
}
