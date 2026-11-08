using AgentPlatform.Application.Abstractions;
using AgentPlatform.Domain.Agents;

namespace AgentPlatform.Application.Agents;

public sealed record McpToolGrantSummary(Guid McpServerId, string ToolName);
public sealed record AgentSummary(Guid Id, Guid ProjectId, string Name, string Description, string SystemPrompt, Guid? LlmConfigurationId, IReadOnlyList<McpToolGrantSummary> McpToolGrants, DateTimeOffset CreatedAt);

public sealed class AgentService(IProjectRepository projects, IAgentRepository agents, ILlmConfigurationRepository llmConfigurations, IMcpServerRepository mcpServers, IWebhookEventPublisher webhookEvents, TimeProvider timeProvider)
{
    public async Task<IReadOnlyList<AgentSummary>?> ListAsync(Guid projectId, CancellationToken cancellationToken)
    {
        if (await projects.FindByIdAsync(projectId, cancellationToken) is null) return null;
        return (await agents.ListByProjectAsync(projectId, cancellationToken)).Select(ToSummary).ToArray();
    }

    public async Task<AgentSummary?> CreateAsync(Guid projectId, string name, string description, string systemPrompt, Guid? llmConfigurationId, IReadOnlyList<McpToolGrantSummary> mcpToolGrants, CancellationToken cancellationToken)
    {
        if (await projects.FindByIdAsync(projectId, cancellationToken) is null) return null;
        await ValidateConfigurationAsync(projectId, llmConfigurationId, mcpToolGrants, cancellationToken);
        var agent = new AgentDefinition(Guid.NewGuid(), projectId, name, description, systemPrompt, llmConfigurationId, timeProvider.GetUtcNow());
        agent.ReplaceMcpToolGrants(mcpToolGrants.Select(item => (item.McpServerId, item.ToolName)));
        await agents.AddAsync(agent, cancellationToken);
        await webhookEvents.EnqueueAsync(projectId, AgentPlatform.Domain.Webhooks.WebhookEventTypes.AgentCreated, ToSummary(agent), cancellationToken);
        await agents.SaveChangesAsync(cancellationToken);
        return ToSummary(agent);
    }

    public async Task<AgentSummary?> UpdateAsync(Guid projectId, Guid agentId, string name, string description, string systemPrompt, Guid? llmConfigurationId, IReadOnlyList<McpToolGrantSummary> mcpToolGrants, CancellationToken cancellationToken)
    {
        var agent = await agents.FindByIdAsync(projectId, agentId, cancellationToken);
        if (agent is null) return null;
        await ValidateConfigurationAsync(projectId, llmConfigurationId, mcpToolGrants, cancellationToken);
        agent.Update(name, description, systemPrompt, llmConfigurationId);
        agent.ReplaceMcpToolGrants(mcpToolGrants.Select(item => (item.McpServerId, item.ToolName)));
        await webhookEvents.EnqueueAsync(projectId, AgentPlatform.Domain.Webhooks.WebhookEventTypes.AgentUpdated, ToSummary(agent), cancellationToken);
        await agents.SaveChangesAsync(cancellationToken);
        return ToSummary(agent);
    }

    public async Task<bool> DeleteAsync(Guid projectId, Guid agentId, CancellationToken cancellationToken)
    {
        var agent = await agents.FindByIdAsync(projectId, agentId, cancellationToken);
        if (agent is null) return false;
        await webhookEvents.EnqueueAsync(projectId, AgentPlatform.Domain.Webhooks.WebhookEventTypes.AgentDeleted, ToSummary(agent), cancellationToken);
        agents.Remove(agent);
        await agents.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task ValidateConfigurationAsync(Guid projectId, Guid? llmConfigurationId, IReadOnlyList<McpToolGrantSummary> mcpToolGrants, CancellationToken cancellationToken)
    {
        if (llmConfigurationId is not null && await llmConfigurations.FindAsync(llmConfigurationId.Value, cancellationToken) is null)
            throw new ArgumentException("The selected LLM configuration no longer exists.", nameof(llmConfigurationId));

        foreach (var serverId in mcpToolGrants.Select(item => item.McpServerId).Distinct())
            if (await mcpServers.FindByIdAsync(projectId, serverId, cancellationToken) is null)
                throw new ArgumentException("Each MCP tool must belong to a server in this project.", nameof(mcpToolGrants));
    }

    private static AgentSummary ToSummary(AgentDefinition agent) => new(agent.Id, agent.ProjectId, agent.Name, agent.Description, agent.SystemPrompt, agent.LlmConfigurationId, agent.McpToolGrants.Select(item => new McpToolGrantSummary(item.McpServerId, item.ToolName)).ToArray(), agent.CreatedAt);
}
