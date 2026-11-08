namespace AgentPlatform.Domain.Instances;

public sealed record AgentInstance(
    Guid Id,
    Guid AgentVersionId,
    AgentInstanceStatus Status,
    DateTimeOffset CreatedAt);

public enum AgentInstanceStatus
{
    Created,
    Ready,
    Running,
    AwaitingInput,
    Paused,
    Completed,
    Failed,
    Cancelled,
    Expired,
}
