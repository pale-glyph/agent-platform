using AgentPlatform.Application.Abstractions;
using AgentPlatform.Domain.Metrics;

namespace AgentPlatform.Application.Metrics;

public sealed class UsageMetricService(IUsageMetricRepository metrics, IProjectRepository projects, IAgentRepository agents, ILlmConfigurationRepository llmConfigurations)
{
    public Task<IReadOnlyList<UsageMetricBucket>> ListPlatformAsync(DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct) =>
        metrics.ListAsync(UsageMetricScope.Platform, Guid.Empty, from, to, ct);

    public async Task<IReadOnlyList<UsageMetricBucket>?> ListProjectAsync(Guid projectId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct) =>
        await projects.FindByIdAsync(projectId, ct) is null ? null : await metrics.ListAsync(UsageMetricScope.Project, projectId, from, to, ct);

    public async Task<IReadOnlyList<UsageMetricBucket>?> ListAgentAsync(Guid projectId, Guid agentId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct) =>
        await agents.FindByIdAsync(projectId, agentId, ct) is null ? null : await metrics.ListAsync(UsageMetricScope.Agent, agentId, from, to, ct);

    public async Task<IReadOnlyList<UsageMetricBucket>?> ListLlmAsync(Guid llmConfigurationId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct) =>
        await llmConfigurations.FindAsync(llmConfigurationId, ct) is null ? null : await metrics.ListAsync(UsageMetricScope.Llm, llmConfigurationId, from, to, ct);
}
