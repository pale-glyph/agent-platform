namespace AgentPlatform.Domain.Webhooks;

public static class WebhookEventTypes
{
    public const string ProjectUpdated = "project.updated";
    public const string AgentCreated = "agent.created";
    public const string AgentUpdated = "agent.updated";
    public const string AgentDeleted = "agent.deleted";
    public const string McpServerCreated = "mcp_server.created";
    public const string McpServerUpdated = "mcp_server.updated";
    public const string McpServerDeleted = "mcp_server.deleted";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        ProjectUpdated, AgentCreated, AgentUpdated, AgentDeleted,
        McpServerCreated, McpServerUpdated, McpServerDeleted,
    };
}
