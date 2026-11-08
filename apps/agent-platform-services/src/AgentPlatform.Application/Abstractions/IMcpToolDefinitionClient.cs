using AgentPlatform.Domain.Mcp;

namespace AgentPlatform.Application.Abstractions;

public interface IMcpToolDefinitionClient
{
    Task<string> ListToolsAsync(McpServer server, CancellationToken cancellationToken);
}
