using AgentPlatform.Application.Abstractions;
using AgentPlatform.Domain.Agents;

namespace AgentPlatform.Application.Agents;

public sealed record AgentSummary(Guid Id, Guid ProjectId, string Name, string Description, string SystemPrompt, DateTimeOffset CreatedAt);

public sealed class AgentService(IProjectRepository projects, IAgentRepository agents, TimeProvider timeProvider)
{
    public async Task<IReadOnlyList<AgentSummary>?> ListAsync(Guid projectId, CancellationToken cancellationToken)
    {
        if (await projects.FindByIdAsync(projectId, cancellationToken) is null) return null;
        return (await agents.ListByProjectAsync(projectId, cancellationToken)).Select(ToSummary).ToArray();
    }

    public async Task<AgentSummary?> CreateAsync(Guid projectId, string name, string description, string systemPrompt, CancellationToken cancellationToken)
    {
        if (await projects.FindByIdAsync(projectId, cancellationToken) is null) return null;
        var agent = new AgentDefinition(Guid.NewGuid(), projectId, name, description, systemPrompt, timeProvider.GetUtcNow());
        await agents.AddAsync(agent, cancellationToken);
        await agents.SaveChangesAsync(cancellationToken);
        return ToSummary(agent);
    }

    public async Task<AgentSummary?> UpdateAsync(Guid projectId, Guid agentId, string name, string description, string systemPrompt, CancellationToken cancellationToken)
    {
        var agent = await agents.FindByIdAsync(projectId, agentId, cancellationToken);
        if (agent is null) return null;
        agent.Update(name, description, systemPrompt);
        await agents.SaveChangesAsync(cancellationToken);
        return ToSummary(agent);
    }

    public async Task<bool> DeleteAsync(Guid projectId, Guid agentId, CancellationToken cancellationToken)
    {
        var agent = await agents.FindByIdAsync(projectId, agentId, cancellationToken);
        if (agent is null) return false;
        agents.Remove(agent);
        await agents.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static AgentSummary ToSummary(AgentDefinition agent) => new(agent.Id, agent.ProjectId, agent.Name, agent.Description, agent.SystemPrompt, agent.CreatedAt);
}
