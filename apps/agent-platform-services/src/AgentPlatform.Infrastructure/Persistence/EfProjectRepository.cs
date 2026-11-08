using AgentPlatform.Application.Abstractions;
using AgentPlatform.Domain.Projects;
using Microsoft.EntityFrameworkCore;

namespace AgentPlatform.Infrastructure.Persistence;

public sealed class EfProjectRepository(PlatformDbContext dbContext) : IProjectRepository
{
    public async Task<IReadOnlyList<Project>> ListAsync(CancellationToken cancellationToken) =>
        await dbContext.Projects.AsNoTracking().OrderBy(item => item.Name).ToArrayAsync(cancellationToken);

    public Task<Project?> FindByIdAsync(Guid projectId, CancellationToken cancellationToken) =>
        dbContext.Projects.SingleOrDefaultAsync(item => item.Id == projectId, cancellationToken);

    public Task AddAsync(Project project, CancellationToken cancellationToken) =>
        dbContext.Projects.AddAsync(project, cancellationToken).AsTask();

    public void Remove(Project project) => dbContext.Projects.Remove(project);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}
