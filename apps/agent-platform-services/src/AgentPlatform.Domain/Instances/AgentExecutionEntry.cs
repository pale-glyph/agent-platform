namespace AgentPlatform.Domain.Instances;

public sealed class AgentExecutionEntry
{
    public const int MaximumContentLength = 100_000;
    public const int MaximumToolNameLength = 500;

    private AgentExecutionEntry(Guid id, Guid instanceId, long sequenceNumber, AgentExecutionEntryType type, DateTimeOffset createdAt)
    { Id = id; InstanceId = instanceId; SequenceNumber = sequenceNumber; Type = type; CreatedAt = createdAt; }

    public Guid Id { get; }
    public Guid InstanceId { get; }
    public long SequenceNumber { get; }
    public AgentExecutionEntryType Type { get; }
    public AgentMessageRole? Role { get; private set; }
    public AgentMessageDelivery? Delivery { get; private set; }
    public AgentMessageStatus? MessageStatus { get; private set; }
    public string? Content { get; private set; }
    public string? ToolName { get; private set; }
    public string? ToolArguments { get; private set; }
    public string? ToolResult { get; private set; }
    public DateTimeOffset CreatedAt { get; }

    public static AgentExecutionEntry Message(Guid id, Guid instanceId, long sequenceNumber, AgentMessageRole role, AgentMessageDelivery delivery, AgentMessageStatus status, string content, DateTimeOffset createdAt) =>
        new(id, instanceId, sequenceNumber, AgentExecutionEntryType.Message, createdAt) { Role = role, Delivery = delivery, MessageStatus = status, Content = Normalize(content, "Message content") };

    public static AgentExecutionEntry ToolCall(Guid id, Guid instanceId, long sequenceNumber, string toolName, string arguments, string? result, DateTimeOffset createdAt) =>
        new(id, instanceId, sequenceNumber, AgentExecutionEntryType.ToolCall, createdAt) { ToolName = Normalize(toolName, "Tool name", MaximumToolNameLength), ToolArguments = Normalize(arguments, "Tool arguments"), ToolResult = result is null ? null : Normalize(result, "Tool result") };

    public void MarkDelivered()
    {
        if (Type != AgentExecutionEntryType.Message || MessageStatus != AgentMessageStatus.Pending)
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

public enum AgentExecutionEntryType { Message, ToolCall }
public enum AgentMessageRole { User, Agent }
public enum AgentMessageDelivery { Initial, Queued, Steering }
public enum AgentMessageStatus { Pending, Delivered, Cancelled }
