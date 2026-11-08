using AgentPlatform.Domain.Instances;

namespace AgentPlatform.Application.Abstractions;

public interface IAgentInstanceRepository
{
    Task AddAsync(AgentInstance instance, CancellationToken cancellationToken);
    Task AddEntryAsync(AgentExecutionEntry entry, CancellationToken cancellationToken);
    void Remove(AgentInstance instance);
    Task<AgentInstance?> FindAsync(Guid projectId, Guid instanceId, CancellationToken cancellationToken);
    Task<IReadOnlyList<AgentInstance>> ListRunningAsync(int take, CancellationToken cancellationToken);
    Task<IReadOnlyList<AgentInstance>> ListByProjectAsync(Guid projectId, CancellationToken cancellationToken);
    Task<IReadOnlyList<AgentExecutionEntry>> ListEntriesAsync(Guid instanceId, long? afterSequence, int take, CancellationToken cancellationToken);
    Task<IReadOnlyList<AgentExecutionEntry>> ListPendingMessagesAsync(Guid instanceId, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
