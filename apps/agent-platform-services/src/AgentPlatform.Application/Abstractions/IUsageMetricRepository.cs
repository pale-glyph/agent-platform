using AgentPlatform.Domain.Metrics;

namespace AgentPlatform.Application.Abstractions;

public interface IUsageMetricRepository
{
    Task RecordCompletionAsync(Guid projectId, Guid agentId, Guid llmConfigurationId, DateTimeOffset occurredAt, long inputTokens, long cachedInputTokens, long outputTokens, long reasoningTokens, long totalTokens, CancellationToken cancellationToken);
    Task<IReadOnlyList<UsageMetricBucket>> ListAsync(UsageMetricScope scope, Guid scopeId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken cancellationToken);
}
