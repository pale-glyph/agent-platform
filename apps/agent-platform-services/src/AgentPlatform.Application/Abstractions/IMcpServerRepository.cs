using AgentPlatform.Domain.Mcp;

namespace AgentPlatform.Application.Abstractions;

public interface IMcpServerRepository
{
    Task<IReadOnlyList<McpServer>> ListByProjectAsync(Guid projectId, CancellationToken cancellationToken);
    Task<McpServer?> FindByIdAsync(Guid projectId, Guid serverId, CancellationToken cancellationToken);
    Task AddAsync(McpServer server, CancellationToken cancellationToken);
    void Remove(McpServer server);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
