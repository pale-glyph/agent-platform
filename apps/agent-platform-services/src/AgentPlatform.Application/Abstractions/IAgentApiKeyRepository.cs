using AgentPlatform.Domain.Agents;

namespace AgentPlatform.Application.Abstractions;

public sealed record AgentApiKeyAuthentication(Guid AgentId, Guid ProjectId);

public interface IAgentApiKeyRepository
{
    Task<IReadOnlyList<AgentApiKey>> ListByAgentAsync(Guid agentId, CancellationToken cancellationToken);
    Task<AgentApiKey?> FindByIdAsync(Guid agentId, Guid keyId, CancellationToken cancellationToken);
    Task<AgentApiKeyAuthentication?> FindAuthenticationAsync(string keyHash, CancellationToken cancellationToken);
    Task AddAsync(AgentApiKey key, CancellationToken cancellationToken);
    void Remove(AgentApiKey key);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
