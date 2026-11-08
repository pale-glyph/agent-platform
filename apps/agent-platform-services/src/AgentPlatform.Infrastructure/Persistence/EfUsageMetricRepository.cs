using AgentPlatform.Application.Abstractions;
using AgentPlatform.Domain.Metrics;
using Microsoft.EntityFrameworkCore;

namespace AgentPlatform.Infrastructure.Persistence;

public sealed class EfUsageMetricRepository(PlatformDbContext db) : IUsageMetricRepository
{
    public async Task RecordCompletionAsync(Guid projectId, Guid agentId, Guid llmConfigurationId, DateTimeOffset occurredAt, long inputTokens, long cachedInputTokens, long outputTokens, long reasoningTokens, long totalTokens, CancellationToken ct)
    {
        var bucketStart = FloorToQuarterHour(occurredAt);
        foreach (var (scope, scopeId) in new[]
        {
            (UsageMetricScope.Platform, Guid.Empty),
            (UsageMetricScope.Project, projectId),
            (UsageMetricScope.Agent, agentId),
            (UsageMetricScope.Llm, llmConfigurationId),
        })
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO usage_metric_buckets (scope, scope_id, bucket_start, call_count, input_tokens, cached_input_tokens, output_tokens, reasoning_tokens, total_tokens)
                VALUES ({scope.ToString()}, {scopeId}, {bucketStart}, 1, {inputTokens}, {cachedInputTokens}, {outputTokens}, {reasoningTokens}, {totalTokens})
                ON CONFLICT (scope, scope_id, bucket_start) DO UPDATE SET
                    call_count = usage_metric_buckets.call_count + 1,
                    input_tokens = usage_metric_buckets.input_tokens + EXCLUDED.input_tokens,
                    cached_input_tokens = usage_metric_buckets.cached_input_tokens + EXCLUDED.cached_input_tokens,
                    output_tokens = usage_metric_buckets.output_tokens + EXCLUDED.output_tokens,
                    reasoning_tokens = usage_metric_buckets.reasoning_tokens + EXCLUDED.reasoning_tokens,
                    total_tokens = usage_metric_buckets.total_tokens + EXCLUDED.total_tokens;
                """, ct);
        }
    }

    public async Task<IReadOnlyList<UsageMetricBucket>> ListAsync(UsageMetricScope scope, Guid scopeId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct) =>
        await db.UsageMetricBuckets.AsNoTracking()
            .Where(bucket => bucket.Scope == scope && bucket.ScopeId == scopeId && (!from.HasValue || bucket.BucketStart >= from.Value) && (!to.HasValue || bucket.BucketStart < to.Value))
            .OrderBy(bucket => bucket.BucketStart)
            .ToArrayAsync(ct);

    private static DateTimeOffset FloorToQuarterHour(DateTimeOffset timestamp)
    {
        var utc = timestamp.ToUniversalTime();
        return new DateTimeOffset(utc.Year, utc.Month, utc.Day, utc.Hour, utc.Minute / 15 * 15, 0, TimeSpan.Zero);
    }
}
