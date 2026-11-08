using AgentPlatform.Domain.Metrics;

namespace AgentPlatform.Application.Metrics;

public sealed record UsageMetricBucketSummary(DateTimeOffset BucketStart, long CallCount, long InputTokens, long CachedInputTokens, long OutputTokens, long ReasoningTokens, long TotalTokens)
{
    public static UsageMetricBucketSummary From(UsageMetricBucket bucket) => new(bucket.BucketStart, bucket.CallCount, bucket.InputTokens, bucket.CachedInputTokens, bucket.OutputTokens, bucket.ReasoningTokens, bucket.TotalTokens);
}
