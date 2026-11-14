using AgentPlatform.Application.Abstractions;
using AgentPlatform.Domain.Threads;
using AgentPlatform.Domain.Webhooks;

namespace AgentPlatform.Application.Threads;

public sealed record AgentThreadSummary(Guid Id, Guid ProjectId, Guid AgentId, AgentThreadStatus Status, bool HasPendingQueuedMessage, DateTimeOffset? CancellationRequestedAt, DateTimeOffset CreatedAt);
public sealed record AgentThreadEntrySummary(Guid Id, Guid ThreadId, long SequenceNumber, AgentThreadEntryType Type, AgentMessageRole? Role, AgentMessageDelivery? Delivery, AgentMessageStatus? MessageStatus, string? Content, string? ToolName, string? ToolArguments, string? ToolResult, DateTimeOffset CreatedAt);
public sealed record AgentThreadPage(IReadOnlyList<AgentThreadEntrySummary> Items, long? NextCursor);
public sealed record SubmittedThreadMessage(AgentThreadEntrySummary Entry, bool CancelledInFlightRequest);

public sealed class AgentThreadService(IAgentRepository agents, IAgentThreadRepository threads, IWebhookEventPublisher webhookEvents, TimeProvider timeProvider)
{
    public async Task<IReadOnlyList<AgentThreadSummary>> ListAsync(Guid projectId, CancellationToken cancellationToken) =>
        (await threads.ListByProjectAsync(projectId, cancellationToken)).Select(ToSummary).ToArray();

    public async Task<AgentThreadSummary?> TriggerAsync(Guid projectId, Guid agentId, string initialPrompt, CancellationToken cancellationToken)
    {
        if (await agents.FindByIdAsync(projectId, agentId, cancellationToken) is null) return null;
        var now = timeProvider.GetUtcNow();
        var thread = new AgentThread(Guid.NewGuid(), projectId, agentId, now);
        var message = AgentThreadEntry.Message(Guid.NewGuid(), thread.Id, thread.AllocateSequenceNumber(), AgentMessageRole.User, AgentMessageDelivery.Initial, AgentMessageStatus.Delivered, initialPrompt, now);
        thread.Start();
        await threads.AddAsync(thread, cancellationToken);
        await threads.AddEntryAsync(message, cancellationToken);
        await webhookEvents.EnqueueAsync(projectId, WebhookEventTypes.AgentMessage, ToSummary(message), cancellationToken);
        await threads.SaveChangesAsync(cancellationToken);
        return ToSummary(thread);
    }

    public async Task<AgentThreadPage?> ListEntriesAsync(Guid projectId, Guid threadId, long? afterSequence, int limit, CancellationToken cancellationToken)
    {
        if (limit is < 1 or > 100) throw new ArgumentOutOfRangeException(nameof(limit), "Limit must be between 1 and 100.");
        if (afterSequence is < 0) throw new ArgumentOutOfRangeException(nameof(afterSequence), "Cursor must not be negative.");
        if (await threads.FindAsync(projectId, threadId, cancellationToken) is null) return null;
        var entries = await threads.ListEntriesAsync(threadId, afterSequence, limit + 1, cancellationToken);
        var hasMore = entries.Count > limit;
        var page = entries.Take(limit).Select(ToSummary).ToArray();
        return new AgentThreadPage(page, hasMore ? page[^1].SequenceNumber : null);
    }

    public async Task<bool> DeleteAsync(Guid projectId, Guid threadId, CancellationToken cancellationToken)
    {
        var thread = await threads.FindAsync(projectId, threadId, cancellationToken);
        if (thread is null) return false;
        threads.Remove(thread);
        await threads.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<SubmittedThreadMessage?> SubmitMessageAsync(Guid projectId, Guid threadId, string content, AgentMessageDelivery delivery, CancellationToken cancellationToken)
    {
        if (delivery == AgentMessageDelivery.Initial) throw new ArgumentException("Initial delivery can only be used when triggering a thread.", nameof(delivery));
        var thread = await threads.FindAsync(projectId, threadId, cancellationToken);
        if (thread is null) return null;
        var now = timeProvider.GetUtcNow();
        var resumed = thread.Resume();
        var status = delivery == AgentMessageDelivery.Queued ? AgentMessageStatus.Pending : AgentMessageStatus.Delivered;
        var entry = AgentThreadEntry.Message(Guid.NewGuid(), thread.Id, thread.AllocateSequenceNumber(), AgentMessageRole.User, delivery, status, content, now);
        if (delivery == AgentMessageDelivery.Queued) thread.QueueMessage(entry.Id);
        var cancellationRequested = delivery == AgentMessageDelivery.Steering && !resumed && thread.RequestCancellation(now);
        await threads.AddEntryAsync(entry, cancellationToken);
        await webhookEvents.EnqueueAsync(projectId, WebhookEventTypes.AgentMessage, ToSummary(entry), cancellationToken);
        await threads.SaveChangesAsync(cancellationToken);
        return new SubmittedThreadMessage(ToSummary(entry), cancellationRequested);
    }

    public async Task<AgentThreadEntrySummary?> RecordToolCallAsync(Guid projectId, Guid threadId, string toolName, string arguments, string? result, CancellationToken cancellationToken)
    {
        var thread = await threads.FindAsync(projectId, threadId, cancellationToken);
        if (thread is null) return null;
        var entry = AgentThreadEntry.ToolCall(Guid.NewGuid(), thread.Id, thread.AllocateSequenceNumber(), toolName, arguments, result, timeProvider.GetUtcNow());
        await threads.AddEntryAsync(entry, cancellationToken);
        await webhookEvents.EnqueueAsync(projectId, WebhookEventTypes.AgentToolCall, ToSummary(entry), cancellationToken);
        await threads.SaveChangesAsync(cancellationToken);
        return ToSummary(entry);
    }

    private static AgentThreadSummary ToSummary(AgentThread item) => new(item.Id, item.ProjectId, item.AgentId, item.Status, item.PendingQueuedMessageId is not null, item.CancellationRequestedAt, item.CreatedAt);
    private static AgentThreadEntrySummary ToSummary(AgentThreadEntry item) => new(item.Id, item.ThreadId, item.SequenceNumber, item.Type, item.Role, item.Delivery, item.MessageStatus, item.Content, item.ToolName, item.ToolArguments, item.ToolResult, item.CreatedAt);
}
