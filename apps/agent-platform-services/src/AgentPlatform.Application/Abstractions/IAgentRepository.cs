using AgentPlatform.Domain.Agents;

namespace AgentPlatform.Application.Abstractions;

public interface IAgentRepository
{
    Task<IReadOnlyList<AgentDefinition>> ListByProjectAsync(Guid projectId, CancellationToken cancellationToken);
    Task<AgentDefinition?> FindByIdAsync(Guid projectId, Guid agentId, CancellationToken cancellationToken);
    Task AddAsync(AgentDefinition agent, CancellationToken cancellationToken);
    void Remove(AgentDefinition agent);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
