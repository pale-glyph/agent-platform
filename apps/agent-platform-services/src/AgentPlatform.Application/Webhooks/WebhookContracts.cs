using AgentPlatform.Domain.Webhooks;

namespace AgentPlatform.Application.Webhooks;

public sealed record WebhookSummary(Guid Id, Guid ProjectId, string Name, string EndpointUrl, IReadOnlyList<string> Events, bool HasSigningSecret, bool IsEnabled, DateTimeOffset CreatedAt);
public sealed record WebhookDeliverySummary(Guid Id, Guid WebhookId, string WebhookName, string EventType, WebhookDeliveryStatus Status, int AttemptCount, int? LastStatusCode, string? LastError, DateTimeOffset CreatedAt, DateTimeOffset NextAttemptAt, DateTimeOffset? CompletedAt);
public sealed record WebhookMetricsSummary(long Sent, long Failed, long Pending, long Attempts);
