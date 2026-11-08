using AgentPlatform.Application.Abstractions;
using AgentPlatform.Domain.Projects;

namespace AgentPlatform.Application.Projects;

public sealed class ProjectService(IProjectRepository projects, TimeProvider timeProvider)
{
    public async Task<IReadOnlyList<ProjectSummary>> ListAsync(CancellationToken cancellationToken)
    {
        var items = await projects.ListAsync(cancellationToken);
        return items.Select(ToSummary).ToArray();
    }

    public async Task<ProjectSummary?> FindAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var project = await projects.FindByIdAsync(projectId, cancellationToken);
        return project is null ? null : ToSummary(project);
    }

    public async Task<ProjectSummary> CreateAsync(string name, CancellationToken cancellationToken)
    {
        var project = new Project(Guid.NewGuid(), name, timeProvider.GetUtcNow());
        await projects.AddAsync(project, cancellationToken);
        await projects.SaveChangesAsync(cancellationToken);
        return ToSummary(project);
    }

    public async Task<ProjectSummary?> UpdateAsync(Guid projectId, string name, CancellationToken cancellationToken)
    {
        var project = await projects.FindByIdAsync(projectId, cancellationToken);
        if (project is null)
        {
            return null;
        }

        project.Rename(name);
        await projects.SaveChangesAsync(cancellationToken);
        return ToSummary(project);
    }

    public async Task<bool> DeleteAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var project = await projects.FindByIdAsync(projectId, cancellationToken);
        if (project is null)
        {
            return false;
        }

        projects.Remove(project);
        await projects.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static ProjectSummary ToSummary(Project project) => new(project.Id, project.Name, project.CreatedAt);
}
