using AgentPlatform.Application.Abstractions;
using AgentPlatform.Domain.Webhooks;

namespace AgentPlatform.Application.Webhooks;

public enum DeleteWebhookDeliveryResult
{
    Deleted,
    NotFound,
    NotFailed,
}

public sealed class WebhookService(IProjectRepository projects, IWebhookRepository webhooks, TimeProvider clock)
{
    public async Task<IReadOnlyList<WebhookSummary>?> ListAsync(Guid projectId, CancellationToken ct)
    {
        if (await projects.FindByIdAsync(projectId, ct) is null) return null;
        return (await webhooks.ListByProjectAsync(projectId, ct)).Select(Map).ToArray();
    }

    public async Task<WebhookSummary?> CreateAsync(Guid projectId, string name, string endpointUrl, string secret, IEnumerable<string> events, CancellationToken ct)
    {
        if (await projects.FindByIdAsync(projectId, ct) is null) return null;
        var item = new WebhookEndpoint(Guid.NewGuid(), projectId, name, endpointUrl, secret, events, clock.GetUtcNow());
        await webhooks.AddAsync(item, ct);
        await webhooks.SaveChangesAsync(ct);
        return Map(item);
    }

    public async Task<WebhookSummary?> UpdateAsync(Guid projectId, Guid webhookId, string name, string endpointUrl, string? secret, IEnumerable<string> events, bool enabled, CancellationToken ct)
    {
        var item = await webhooks.FindAsync(projectId, webhookId, ct);
        if (item is null) return null;
        item.Update(name, endpointUrl, secret, events);
        if (enabled) item.Enable(); else item.Disable();
        if (!enabled) await webhooks.CancelPendingForWebhookAsync(webhookId, clock.GetUtcNow(), ct);
        await webhooks.SaveChangesAsync(ct);
        return Map(item);
    }

    public async Task<bool> DeleteAsync(Guid projectId, Guid webhookId, CancellationToken ct)
    {
        var item = await webhooks.FindAsync(projectId, webhookId, ct);
        if (item is null) return false;
        item.Disable();
        await webhooks.CancelPendingForWebhookAsync(webhookId, clock.GetUtcNow(), ct);
        webhooks.Remove(item);
        await webhooks.SaveChangesAsync(ct);
        return true;
    }

    public async Task<IReadOnlyList<WebhookDeliverySummary>?> ListDeliveriesAsync(Guid projectId, CancellationToken ct)
    {
        if (await projects.FindByIdAsync(projectId, ct) is null) return null;
        return await webhooks.ListDeliveriesAsync(projectId, 100, ct);
    }

    public async Task<WebhookMetricsSummary?> GetMetricsAsync(Guid projectId, CancellationToken ct)
    {
        if (await projects.FindByIdAsync(projectId, ct) is null) return null;
        return await webhooks.GetMetricsAsync(projectId, ct);
    }

    public async Task<int?> RetryFailedAsync(Guid projectId, CancellationToken ct)
    {
        if (await projects.FindByIdAsync(projectId, ct) is null) return null;
        return await webhooks.RetryFailedAsync(projectId, clock.GetUtcNow(), ct);
    }

    public async Task<DeleteWebhookDeliveryResult> DeleteFailedDeliveryAsync(Guid projectId, Guid deliveryId, CancellationToken ct)
    {
        if (await projects.FindByIdAsync(projectId, ct) is null) return DeleteWebhookDeliveryResult.NotFound;
        var delivery = await webhooks.FindDeliveryAsync(projectId, deliveryId, ct);
        if (delivery is null) return DeleteWebhookDeliveryResult.NotFound;
        if (delivery.Status != WebhookDeliveryStatus.Failed) return DeleteWebhookDeliveryResult.NotFailed;

        delivery.Cancel(clock.GetUtcNow());
        await webhooks.SaveChangesAsync(ct);
        return DeleteWebhookDeliveryResult.Deleted;
    }

    private static WebhookSummary Map(WebhookEndpoint item) => new(item.Id, item.ProjectId, item.Name, item.EndpointUrl, item.Events, !string.IsNullOrEmpty(item.Secret), item.IsEnabled, item.CreatedAt);
}
