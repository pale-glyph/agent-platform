using AgentPlatform.Application.Abstractions;
using AgentPlatform.Domain.Instances;
using Microsoft.EntityFrameworkCore;

namespace AgentPlatform.Infrastructure.Persistence;

public sealed class EfAgentInstanceRepository(PlatformDbContext dbContext) : IAgentInstanceRepository
{
    public Task AddAsync(AgentInstance instance, CancellationToken cancellationToken) => dbContext.AgentInstances.AddAsync(instance, cancellationToken).AsTask();
    public Task AddEntryAsync(AgentExecutionEntry entry, CancellationToken cancellationToken) => dbContext.AgentExecutionEntries.AddAsync(entry, cancellationToken).AsTask();
    public Task<AgentInstance?> FindAsync(Guid projectId, Guid instanceId, CancellationToken cancellationToken) => dbContext.AgentInstances.SingleOrDefaultAsync(item => item.ProjectId == projectId && item.Id == instanceId, cancellationToken);
    public async Task<IReadOnlyList<AgentInstance>> ListByProjectAsync(Guid projectId, CancellationToken cancellationToken) => await dbContext.AgentInstances.AsNoTracking().Where(item => item.ProjectId == projectId).OrderByDescending(item => item.CreatedAt).ToArrayAsync(cancellationToken);
    public async Task<IReadOnlyList<AgentExecutionEntry>> ListEntriesAsync(Guid instanceId, long? afterSequence, int take, CancellationToken cancellationToken) => await dbContext.AgentExecutionEntries.AsNoTracking().Where(item => item.InstanceId == instanceId && (!afterSequence.HasValue || item.SequenceNumber > afterSequence.Value)).OrderBy(item => item.SequenceNumber).Take(take).ToArrayAsync(cancellationToken);
    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}
