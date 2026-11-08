namespace AgentPlatform.Domain.Agents;

public sealed record AgentVersion(
    Guid Id,
    Guid AgentId,
    int VersionNumber,
    string Instructions,
    AgentVersionStatus Status,
    DateTimeOffset CreatedAt);

public enum AgentVersionStatus
{
    Draft,
    Published,
    Archived,
}
