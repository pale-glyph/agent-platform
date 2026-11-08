using AgentPlatform.Domain.Projects;

namespace AgentPlatform.Application.Abstractions;

public interface IProjectRepository
{
    Task<IReadOnlyList<Project>> ListAsync(CancellationToken cancellationToken);
    Task<Project?> FindByIdAsync(Guid projectId, CancellationToken cancellationToken);
    Task AddAsync(Project project, CancellationToken cancellationToken);
    void Remove(Project project);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
