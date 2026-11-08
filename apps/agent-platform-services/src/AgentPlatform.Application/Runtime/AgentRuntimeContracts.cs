using AgentPlatform.Domain.Llm;
using AgentPlatform.Domain.Mcp;

namespace AgentPlatform.Application.Runtime;

public sealed record RuntimeChatMessage(string Role, string? Content, string? ToolCallId = null, IReadOnlyList<RuntimeToolCall>? ToolCalls = null);
public sealed record RuntimeToolDefinition(string Name, string Description, string ParametersJson);
public sealed record RuntimeToolCall(string Id, string Name, string ArgumentsJson);
public sealed record RuntimeTokenUsage(long InputTokens, long CachedInputTokens, long OutputTokens, long ReasoningTokens, long TotalTokens)
{
    public static readonly RuntimeTokenUsage Empty = new(0, 0, 0, 0, 0);
}

public sealed record RuntimeCompletion(string? Content, IReadOnlyList<RuntimeToolCall> ToolCalls, RuntimeTokenUsage Usage);

public interface IOpenAiCompatibleClient
{
    Task<RuntimeCompletion> CompleteAsync(LlmConfiguration configuration, IReadOnlyList<RuntimeChatMessage> messages, IReadOnlyList<RuntimeToolDefinition> tools, CancellationToken cancellationToken);
}

public interface IMcpToolClient
{
    Task<string> CallAsync(McpServer server, string toolName, string argumentsJson, CancellationToken cancellationToken);
}
