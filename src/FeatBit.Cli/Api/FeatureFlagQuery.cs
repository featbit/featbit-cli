namespace FeatBit.Cli.Api;

public sealed record FeatureFlagQuery(
    string? Name,
    string? Tags,
    int PageIndex,
    int PageSize);
