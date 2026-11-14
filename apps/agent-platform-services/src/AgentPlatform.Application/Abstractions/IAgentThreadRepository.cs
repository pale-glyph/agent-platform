using AgentPlatform.Domain.Threads;

namespace AgentPlatform.Application.Abstractions;

public interface IAgentThreadRepository
{
    Task AddAsync(AgentThread thread, CancellationToken cancellationToken);
    Task AddEntryAsync(AgentThreadEntry entry, CancellationToken cancellationToken);
    void Remove(AgentThread thread);
    Task<AgentThread?> FindAsync(Guid projectId, Guid threadId, CancellationToken cancellationToken);
    Task<IReadOnlyList<AgentThread>> ListRunningAsync(int take, CancellationToken cancellationToken);
    Task<IReadOnlyList<AgentThread>> ListByProjectAsync(Guid projectId, CancellationToken cancellationToken);
    Task<IReadOnlyList<AgentThreadEntry>> ListEntriesAsync(Guid threadId, long? afterSequence, int take, CancellationToken cancellationToken);
    Task<IReadOnlyList<AgentThreadEntry>> ListPendingMessagesAsync(Guid threadId, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
