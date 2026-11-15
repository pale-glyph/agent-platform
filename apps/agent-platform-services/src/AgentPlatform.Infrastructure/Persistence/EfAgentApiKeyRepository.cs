using AgentPlatform.Application.Abstractions;
using AgentPlatform.Domain.Agents;
using Microsoft.EntityFrameworkCore;

namespace AgentPlatform.Infrastructure.Persistence;

public sealed class EfAgentApiKeyRepository(PlatformDbContext dbContext) : IAgentApiKeyRepository
{
    public async Task<IReadOnlyList<AgentApiKey>> ListByAgentAsync(Guid agentId, CancellationToken cancellationToken) =>
        await dbContext.AgentApiKeys.AsNoTracking().Where(item => item.AgentId == agentId).OrderBy(item => item.CreatedAt).ToArrayAsync(cancellationToken);

    public Task<AgentApiKey?> FindByIdAsync(Guid agentId, Guid keyId, CancellationToken cancellationToken) =>
        dbContext.AgentApiKeys.SingleOrDefaultAsync(item => item.AgentId == agentId && item.Id == keyId, cancellationToken);

    public Task<AgentApiKeyAuthentication?> FindAuthenticationAsync(string keyHash, CancellationToken cancellationToken) =>
        dbContext.AgentApiKeys.Where(key => key.KeyHash == keyHash)
            .Join(dbContext.Agents, key => key.AgentId, agent => agent.Id, (key, agent) => new AgentApiKeyAuthentication(key.AgentId, agent.ProjectId))
            .SingleOrDefaultAsync(cancellationToken);

    public Task AddAsync(AgentApiKey key, CancellationToken cancellationToken) => dbContext.AgentApiKeys.AddAsync(key, cancellationToken).AsTask();
    public void Remove(AgentApiKey key) => dbContext.AgentApiKeys.Remove(key);
    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}
