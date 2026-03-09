namespace FeatBit.Cli.Api;

public sealed record FeatureFlagQuery(
    string? Name,
    int PageIndex,
    int PageSize);
