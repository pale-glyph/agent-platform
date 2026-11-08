namespace AgentPlatform.Domain.Metrics;

/// <summary>A 15-minute aggregate of successful LLM completion usage for one reporting scope.</summary>
public sealed class UsageMetricBucket
{
    public UsageMetricBucket(UsageMetricScope scope, Guid scopeId, DateTimeOffset bucketStart, long callCount, long inputTokens, long cachedInputTokens, long outputTokens, long reasoningTokens, long totalTokens)
    {
        Scope = scope;
        ScopeId = scopeId;
        BucketStart = bucketStart;
        CallCount = callCount;
        InputTokens = inputTokens;
        CachedInputTokens = cachedInputTokens;
        OutputTokens = outputTokens;
        ReasoningTokens = reasoningTokens;
        TotalTokens = totalTokens;
    }

    public UsageMetricScope Scope { get; }
    public Guid ScopeId { get; }
    public DateTimeOffset BucketStart { get; }
    public long CallCount { get; }
    public long InputTokens { get; }
    public long CachedInputTokens { get; }
    public long OutputTokens { get; }
    public long ReasoningTokens { get; }
    public long TotalTokens { get; }
}

public enum UsageMetricScope { Platform, Project, Agent, Llm }
