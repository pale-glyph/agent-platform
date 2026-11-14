using AgentPlatform.Application.Abstractions;
using AgentPlatform.Domain.Threads;
using Microsoft.EntityFrameworkCore;

namespace AgentPlatform.Infrastructure.Persistence;

public sealed class EfAgentThreadRepository(PlatformDbContext dbContext) : IAgentThreadRepository
{
    public Task AddAsync(AgentThread thread, CancellationToken cancellationToken) => dbContext.AgentThreads.AddAsync(thread, cancellationToken).AsTask();
    public Task AddEntryAsync(AgentThreadEntry entry, CancellationToken cancellationToken) => dbContext.AgentThreadEntries.AddAsync(entry, cancellationToken).AsTask();
    public void Remove(AgentThread thread) => dbContext.AgentThreads.Remove(thread);
    public Task<AgentThread?> FindAsync(Guid projectId, Guid threadId, CancellationToken cancellationToken) => dbContext.AgentThreads.SingleOrDefaultAsync(item => item.ProjectId == projectId && item.Id == threadId, cancellationToken);
    public async Task<IReadOnlyList<AgentThread>> ListRunningAsync(int take, CancellationToken cancellationToken) =>
        await dbContext.AgentThreads.Where(item => item.Status == AgentThreadStatus.Running).OrderBy(item => item.CreatedAt).Take(take).ToArrayAsync(cancellationToken);
    public async Task<IReadOnlyList<AgentThread>> ListByProjectAsync(Guid projectId, CancellationToken cancellationToken) => await dbContext.AgentThreads.AsNoTracking().Where(item => item.ProjectId == projectId).OrderByDescending(item => item.CreatedAt).ToArrayAsync(cancellationToken);
    public async Task<IReadOnlyList<AgentThreadEntry>> ListEntriesAsync(Guid threadId, long? afterSequence, int take, CancellationToken cancellationToken) => await dbContext.AgentThreadEntries.AsNoTracking().Where(item => item.ThreadId == threadId && (!afterSequence.HasValue || item.SequenceNumber > afterSequence.Value)).OrderBy(item => item.SequenceNumber).Take(take).ToArrayAsync(cancellationToken);
    public async Task<IReadOnlyList<AgentThreadEntry>> ListPendingMessagesAsync(Guid threadId, CancellationToken cancellationToken) =>
        await dbContext.AgentThreadEntries.Where(item => item.ThreadId == threadId && item.Type == AgentThreadEntryType.Message && item.MessageStatus == AgentMessageStatus.Pending).OrderBy(item => item.SequenceNumber).ToArrayAsync(cancellationToken);
    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}
