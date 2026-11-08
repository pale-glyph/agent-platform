using AgentPlatform.Application.Abstractions;
using AgentPlatform.Domain.Instances;
using AgentPlatform.Domain.Webhooks;

namespace AgentPlatform.Application.Instances;

public sealed record AgentInstanceSummary(Guid Id, Guid ProjectId, Guid AgentId, AgentInstanceStatus Status, bool HasPendingQueuedMessage, DateTimeOffset? CancellationRequestedAt, DateTimeOffset CreatedAt);
public sealed record AgentExecutionEntrySummary(Guid Id, Guid InstanceId, long SequenceNumber, AgentExecutionEntryType Type, AgentMessageRole? Role, AgentMessageDelivery? Delivery, AgentMessageStatus? MessageStatus, string? Content, string? ToolName, string? ToolArguments, string? ToolResult, DateTimeOffset CreatedAt);
public sealed record AgentExecutionPage(IReadOnlyList<AgentExecutionEntrySummary> Items, long? NextCursor);
public sealed record SubmittedMessage(AgentExecutionEntrySummary Entry, bool CancelledInFlightRequest);

public sealed class AgentInstanceService(IAgentRepository agents, IAgentInstanceRepository instances, IWebhookEventPublisher webhookEvents, TimeProvider timeProvider)
{
    public async Task<IReadOnlyList<AgentInstanceSummary>> ListAsync(Guid projectId, CancellationToken cancellationToken) =>
        (await instances.ListByProjectAsync(projectId, cancellationToken)).Select(ToSummary).ToArray();

    public async Task<AgentInstanceSummary?> TriggerAsync(Guid projectId, Guid agentId, string initialPrompt, CancellationToken cancellationToken)
    {
        if (await agents.FindByIdAsync(projectId, agentId, cancellationToken) is null) return null;
        var now = timeProvider.GetUtcNow();
        var instance = new AgentInstance(Guid.NewGuid(), projectId, agentId, now);
        var message = AgentExecutionEntry.Message(Guid.NewGuid(), instance.Id, instance.AllocateSequenceNumber(), AgentMessageRole.User, AgentMessageDelivery.Initial, AgentMessageStatus.Delivered, initialPrompt, now);
        instance.Start();
        await instances.AddAsync(instance, cancellationToken);
        await instances.AddEntryAsync(message, cancellationToken);
        await webhookEvents.EnqueueAsync(projectId, WebhookEventTypes.AgentMessage, ToSummary(message), cancellationToken);
        await instances.SaveChangesAsync(cancellationToken);
        return ToSummary(instance);
    }

    public async Task<AgentExecutionPage?> ListEntriesAsync(Guid projectId, Guid instanceId, long? afterSequence, int limit, CancellationToken cancellationToken)
    {
        if (limit is < 1 or > 100) throw new ArgumentOutOfRangeException(nameof(limit), "Limit must be between 1 and 100.");
        if (afterSequence is < 0) throw new ArgumentOutOfRangeException(nameof(afterSequence), "Cursor must not be negative.");
        if (await instances.FindAsync(projectId, instanceId, cancellationToken) is null) return null;
        var entries = await instances.ListEntriesAsync(instanceId, afterSequence, limit + 1, cancellationToken);
        var hasMore = entries.Count > limit;
        var page = entries.Take(limit).Select(ToSummary).ToArray();
        return new AgentExecutionPage(page, hasMore ? page[^1].SequenceNumber : null);
    }

    public async Task<SubmittedMessage?> SubmitMessageAsync(Guid projectId, Guid instanceId, string content, AgentMessageDelivery delivery, CancellationToken cancellationToken)
    {
        if (delivery == AgentMessageDelivery.Initial) throw new ArgumentException("Initial delivery can only be used when triggering an instance.", nameof(delivery));
        var instance = await instances.FindAsync(projectId, instanceId, cancellationToken);
        if (instance is null) return null;
        var now = timeProvider.GetUtcNow();
        var status = delivery == AgentMessageDelivery.Queued ? AgentMessageStatus.Pending : AgentMessageStatus.Delivered;
        var entry = AgentExecutionEntry.Message(Guid.NewGuid(), instance.Id, instance.AllocateSequenceNumber(), AgentMessageRole.User, delivery, status, content, now);
        if (delivery == AgentMessageDelivery.Queued) instance.QueueMessage(entry.Id);
        var cancellationRequested = delivery == AgentMessageDelivery.Steering && instance.RequestCancellation(now);
        await instances.AddEntryAsync(entry, cancellationToken);
        await webhookEvents.EnqueueAsync(projectId, WebhookEventTypes.AgentMessage, ToSummary(entry), cancellationToken);
        await instances.SaveChangesAsync(cancellationToken);
        return new SubmittedMessage(ToSummary(entry), cancellationRequested);
    }

    public async Task<AgentExecutionEntrySummary?> RecordToolCallAsync(Guid projectId, Guid instanceId, string toolName, string arguments, string? result, CancellationToken cancellationToken)
    {
        var instance = await instances.FindAsync(projectId, instanceId, cancellationToken);
        if (instance is null) return null;
        var entry = AgentExecutionEntry.ToolCall(Guid.NewGuid(), instance.Id, instance.AllocateSequenceNumber(), toolName, arguments, result, timeProvider.GetUtcNow());
        await instances.AddEntryAsync(entry, cancellationToken);
        await webhookEvents.EnqueueAsync(projectId, WebhookEventTypes.AgentToolCall, ToSummary(entry), cancellationToken);
        await instances.SaveChangesAsync(cancellationToken);
        return ToSummary(entry);
    }

    private static AgentInstanceSummary ToSummary(AgentInstance item) => new(item.Id, item.ProjectId, item.AgentId, item.Status, item.PendingQueuedMessageId is not null, item.CancellationRequestedAt, item.CreatedAt);
    private static AgentExecutionEntrySummary ToSummary(AgentExecutionEntry item) => new(item.Id, item.InstanceId, item.SequenceNumber, item.Type, item.Role, item.Delivery, item.MessageStatus, item.Content, item.ToolName, item.ToolArguments, item.ToolResult, item.CreatedAt);
}
