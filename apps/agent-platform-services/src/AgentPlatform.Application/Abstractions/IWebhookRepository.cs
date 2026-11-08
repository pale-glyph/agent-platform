using AgentPlatform.Domain.Webhooks;
using AgentPlatform.Application.Webhooks;

namespace AgentPlatform.Application.Abstractions;

public interface IWebhookRepository
{
    Task<IReadOnlyList<WebhookEndpoint>> ListByProjectAsync(Guid projectId, CancellationToken cancellationToken);
    Task<WebhookEndpoint?> FindAsync(Guid projectId, Guid webhookId, CancellationToken cancellationToken);
    Task AddAsync(WebhookEndpoint webhook, CancellationToken cancellationToken);
    void Remove(WebhookEndpoint webhook);
    Task<IReadOnlyList<WebhookDeliverySummary>> ListDeliveriesAsync(Guid projectId, int limit, CancellationToken cancellationToken);
    Task<WebhookMetricsSummary> GetMetricsAsync(Guid projectId, CancellationToken cancellationToken);
    Task<int> RetryFailedAsync(Guid projectId, DateTimeOffset now, CancellationToken cancellationToken);
    Task CancelPendingForWebhookAsync(Guid webhookId, DateTimeOffset now, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
