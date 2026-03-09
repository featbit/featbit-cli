using FeatBit.Cli.Configuration;
using Xunit;

namespace FeatBit.Cli.Tests;

public sealed class OptionResolverTests
{
    [Fact]
    public void Resolve_ShouldUseEnvironmentValues_WhenCliValuesMissing()
    {
        Environment.SetEnvironmentVariable("FEATBIT_HOST", "https://api.example.com");
        Environment.SetEnvironmentVariable("FEATBIT_TOKEN", "token-123");
        Environment.SetEnvironmentVariable("FEATBIT_ORG", "org-a");

        var options = OptionResolver.Resolve(null, null, null, false);

        Assert.Equal("https://api.example.com", options.Host);
        Assert.Equal("token-123", options.Token);
        Assert.Equal("org-a", options.Organization);
        Assert.False(options.Json);
    }

    [Fact]
    public void Resolve_ShouldThrow_WhenHostMissing()
    {
        Environment.SetEnvironmentVariable("FEATBIT_HOST", null);
        Environment.SetEnvironmentVariable("FEATBIT_TOKEN", "token-123");

        var ex = Assert.Throws<InvalidOperationException>(() => OptionResolver.Resolve(null, null, null, true));
        Assert.Contains("host", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
