using AgentPlatform.Application.Abstractions;
using AgentPlatform.Domain.Mcp;
using Microsoft.EntityFrameworkCore;

namespace AgentPlatform.Infrastructure.Persistence;

public sealed class EfMcpServerRepository(PlatformDbContext dbContext) : IMcpServerRepository
{
    public async Task<IReadOnlyList<McpServer>> ListByProjectAsync(Guid projectId, CancellationToken cancellationToken) =>
        await dbContext.McpServers.AsNoTracking().Where(item => item.ProjectId == projectId).OrderBy(item => item.Name).ToArrayAsync(cancellationToken);
    public Task<McpServer?> FindByIdAsync(Guid projectId, Guid serverId, CancellationToken cancellationToken) =>
        dbContext.McpServers.SingleOrDefaultAsync(item => item.ProjectId == projectId && item.Id == serverId, cancellationToken);
    public Task AddAsync(McpServer server, CancellationToken cancellationToken) => dbContext.McpServers.AddAsync(server, cancellationToken).AsTask();
    public void Remove(McpServer server) => dbContext.McpServers.Remove(server);
    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}
