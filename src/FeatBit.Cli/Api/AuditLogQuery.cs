namespace FeatBit.Cli.Api;

public readonly record struct AuditLogQuery(
    string? Query,
    Guid? CreatorId,
    string? RefId,
    string? RefType,
    long? From,
    long? To,
    bool CrossEnvironment,
    int PageIndex,
    int PageSize);
