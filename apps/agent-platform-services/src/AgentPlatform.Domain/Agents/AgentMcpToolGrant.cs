namespace AgentPlatform.Domain.Agents;

public sealed class AgentMcpToolGrant
{
    public const int MaximumToolNameLength = 500;

    private AgentMcpToolGrant() { }

    public AgentMcpToolGrant(Guid agentId, Guid mcpServerId, string toolName)
    {
        AgentId = agentId;
        McpServerId = mcpServerId;
        ToolName = NormalizeToolName(toolName);
    }

    public Guid AgentId { get; private set; }
    public Guid McpServerId { get; private set; }
    public string ToolName { get; private set; } = null!;

    private static string NormalizeToolName(string value)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized)) throw new ArgumentException("MCP tool name is required.", nameof(value));
        if (normalized.Length > MaximumToolNameLength) throw new ArgumentException($"MCP tool name must be {MaximumToolNameLength} characters or fewer.", nameof(value));
        return normalized;
    }
}
