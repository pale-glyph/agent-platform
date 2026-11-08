using AgentPlatform.Domain.Projects;

namespace AgentPlatform.Application.Abstractions;

public interface IProjectRepository
{
    Task<Project?> FindByIdAsync(Guid projectId, CancellationToken cancellationToken);
}
