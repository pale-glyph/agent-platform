namespace AgentPlatform.Domain.Threads;

public sealed class AgentThread
{
    public AgentThread(Guid id, Guid projectId, Guid agentId, DateTimeOffset createdAt)
    {
        Id = id;
        ProjectId = projectId;
        AgentId = agentId;
        Status = AgentThreadStatus.Ready;
        CreatedAt = createdAt;
    }

    public Guid Id { get; }
    public Guid ProjectId { get; }
    public Guid AgentId { get; }
    public AgentThreadStatus Status { get; private set; }
    public long NextSequenceNumber { get; private set; } = 1;
    public Guid? PendingQueuedMessageId { get; private set; }
    public DateTimeOffset? CancellationRequestedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; }

    public long AllocateSequenceNumber() => NextSequenceNumber++;
    public void Start() => Status = AgentThreadStatus.Running;
    public void Complete()
    {
        if (Status != AgentThreadStatus.Running) throw new InvalidOperationException("Only a running thread can complete.");
        Status = AgentThreadStatus.Completed;
    }

    public bool Resume()
    {
        if (Status != AgentThreadStatus.Completed) return false;
        Status = AgentThreadStatus.Running;
        CancellationRequestedAt = null;
        return true;
    }

    public void Fail()
    {
        if (Status == AgentThreadStatus.Completed) throw new InvalidOperationException("A completed thread cannot fail.");
        Status = AgentThreadStatus.Failed;
    }

    public void QueueMessage(Guid messageId)
    {
        if (PendingQueuedMessageId is not null) throw new InvalidOperationException("A queued message is already pending for this thread.");
        PendingQueuedMessageId = messageId;
    }

    public void MarkQueuedMessageDelivered(Guid messageId)
    {
        if (PendingQueuedMessageId == messageId) PendingQueuedMessageId = null;
    }

    public bool RequestCancellation(DateTimeOffset requestedAt)
    {
        if (Status != AgentThreadStatus.Running || CancellationRequestedAt is not null) return false;
        CancellationRequestedAt = requestedAt;
        return true;
    }
}

public enum AgentThreadStatus
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
