namespace AgentPlatform.Domain.Instances;

public sealed class AgentInstance
{
    public AgentInstance(Guid id, Guid projectId, Guid agentId, DateTimeOffset createdAt)
    {
        Id = id;
        ProjectId = projectId;
        AgentId = agentId;
        Status = AgentInstanceStatus.Ready;
        CreatedAt = createdAt;
    }

    public Guid Id { get; }
    public Guid ProjectId { get; }
    public Guid AgentId { get; }
    public AgentInstanceStatus Status { get; private set; }
    public long NextSequenceNumber { get; private set; } = 1;
    public Guid? PendingQueuedMessageId { get; private set; }
    public DateTimeOffset? CancellationRequestedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; }

    public long AllocateSequenceNumber() => NextSequenceNumber++;
    public void Start() => Status = AgentInstanceStatus.Running;
    public void Complete()
    {
        if (Status != AgentInstanceStatus.Running) throw new InvalidOperationException("Only a running instance can complete.");
        Status = AgentInstanceStatus.Completed;
    }

    public bool Resume()
    {
        if (Status != AgentInstanceStatus.Completed) return false;
        Status = AgentInstanceStatus.Running;
        CancellationRequestedAt = null;
        return true;
    }

    public void Fail()
    {
        if (Status == AgentInstanceStatus.Completed) throw new InvalidOperationException("A completed instance cannot fail.");
        Status = AgentInstanceStatus.Failed;
    }

    public void QueueMessage(Guid messageId)
    {
        if (PendingQueuedMessageId is not null) throw new InvalidOperationException("A queued message is already pending for this instance.");
        PendingQueuedMessageId = messageId;
    }

    public void MarkQueuedMessageDelivered(Guid messageId)
    {
        if (PendingQueuedMessageId == messageId) PendingQueuedMessageId = null;
    }

    public bool RequestCancellation(DateTimeOffset requestedAt)
    {
        if (Status != AgentInstanceStatus.Running || CancellationRequestedAt is not null) return false;
        CancellationRequestedAt = requestedAt;
        return true;
    }
}

public enum AgentInstanceStatus
{
    Ready,
    Running,
    AwaitingInput,
    Paused,
    Completed,
    Failed,
    Cancelled,
    Expired,
}
