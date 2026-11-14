namespace AgentPlatform.Domain.Threads;

public sealed class AgentThreadEntry
{
    public const int MaximumContentLength = 100_000;
    public const int MaximumToolNameLength = 500;

    private AgentThreadEntry(Guid id, Guid threadId, long sequenceNumber, AgentThreadEntryType type, DateTimeOffset createdAt)
    { Id = id; ThreadId = threadId; SequenceNumber = sequenceNumber; Type = type; CreatedAt = createdAt; }

    public Guid Id { get; }
    public Guid ThreadId { get; }
    public long SequenceNumber { get; }
    public AgentThreadEntryType Type { get; }
    public AgentMessageRole? Role { get; private set; }
    public AgentMessageDelivery? Delivery { get; private set; }
    public AgentMessageStatus? MessageStatus { get; private set; }
    public string? Content { get; private set; }
    public string? ToolName { get; private set; }
    public string? ToolArguments { get; private set; }
    public string? ToolResult { get; private set; }
    public DateTimeOffset CreatedAt { get; }

    public static AgentThreadEntry Message(Guid id, Guid threadId, long sequenceNumber, AgentMessageRole role, AgentMessageDelivery delivery, AgentMessageStatus status, string content, DateTimeOffset createdAt) =>
        new(id, threadId, sequenceNumber, AgentThreadEntryType.Message, createdAt) { Role = role, Delivery = delivery, MessageStatus = status, Content = Normalize(content, "Message content") };

    public static AgentThreadEntry ToolCall(Guid id, Guid threadId, long sequenceNumber, string toolName, string arguments, string? result, DateTimeOffset createdAt) =>
        new(id, threadId, sequenceNumber, AgentThreadEntryType.ToolCall, createdAt) { ToolName = Normalize(toolName, "Tool name", MaximumToolNameLength), ToolArguments = Normalize(arguments, "Tool arguments"), ToolResult = result is null ? null : Normalize(result, "Tool result") };

    public void MarkDelivered()
    {
        if (Type != AgentThreadEntryType.Message || MessageStatus != AgentMessageStatus.Pending)
            throw new InvalidOperationException("Only a pending message can be delivered.");
        MessageStatus = AgentMessageStatus.Delivered;
    }

    private static string Normalize(string value, string fieldName, int maximumLength = MaximumContentLength)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException($"{fieldName} is required.", nameof(value));
        if (value.Length > maximumLength) throw new ArgumentException($"{fieldName} must be {maximumLength} characters or fewer.", nameof(value));
        return value;
    }
}

public enum AgentThreadEntryType { Message, ToolCall }
public enum AgentMessageRole { User, Agent }
public enum AgentMessageDelivery { Initial, Queued, Steering }
public enum AgentMessageStatus { Pending, Delivered, Cancelled }
