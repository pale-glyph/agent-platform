using AgentPlatform.Application.Abstractions;
using AgentPlatform.Domain.Agents;
using Microsoft.EntityFrameworkCore;

namespace AgentPlatform.Infrastructure.Persistence;

public sealed class EfAgentRepository(PlatformDbContext dbContext) : IAgentRepository
{
    public async Task<IReadOnlyList<AgentDefinition>> ListByProjectAsync(Guid projectId, CancellationToken cancellationToken) =>
        await dbContext.Agents.AsNoTracking().Include(item => item.McpToolGrants).Where(item => item.ProjectId == projectId).OrderBy(item => item.Name).ToArrayAsync(cancellationToken);

    public Task<AgentDefinition?> FindByIdAsync(Guid projectId, Guid agentId, CancellationToken cancellationToken) =>
        dbContext.Agents.Include(item => item.McpToolGrants).SingleOrDefaultAsync(item => item.ProjectId == projectId && item.Id == agentId, cancellationToken);

    public Task AddAsync(AgentDefinition agent, CancellationToken cancellationToken) => dbContext.Agents.AddAsync(agent, cancellationToken).AsTask();
    public void Remove(AgentDefinition agent) => dbContext.Agents.Remove(agent);
    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}
