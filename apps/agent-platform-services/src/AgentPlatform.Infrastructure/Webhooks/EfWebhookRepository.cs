using AgentPlatform.Application.Abstractions;
using AgentPlatform.Application.Webhooks;
using AgentPlatform.Domain.Webhooks;
using AgentPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AgentPlatform.Infrastructure.Webhooks;

public sealed class EfWebhookRepository(PlatformDbContext db) : IWebhookRepository
{
    public async Task<IReadOnlyList<WebhookEndpoint>> ListByProjectAsync(Guid projectId, CancellationToken ct) =>
        await db.WebhookEndpoints.AsNoTracking().Where(item => item.ProjectId == projectId).OrderBy(item => item.Name).ToArrayAsync(ct);

    public Task<WebhookEndpoint?> FindAsync(Guid projectId, Guid webhookId, CancellationToken ct) =>
        db.WebhookEndpoints.SingleOrDefaultAsync(item => item.ProjectId == projectId && item.Id == webhookId, ct);

    public Task AddAsync(WebhookEndpoint webhook, CancellationToken ct) => db.WebhookEndpoints.AddAsync(webhook, ct).AsTask();
    public void Remove(WebhookEndpoint webhook) => db.WebhookEndpoints.Remove(webhook);

    public async Task<IReadOnlyList<WebhookDeliverySummary>> ListDeliveriesAsync(Guid projectId, int limit, CancellationToken ct) =>
        await db.WebhookDeliveries.AsNoTracking().Where(item => item.ProjectId == projectId && (item.Status == WebhookDeliveryStatus.Pending || item.Status == WebhookDeliveryStatus.Failed))
            .OrderByDescending(item => item.CreatedAt).Take(limit)
            .Select(item => new WebhookDeliverySummary(item.Id, item.WebhookId, item.WebhookName, item.EventType, item.Status, item.AttemptCount, item.LastStatusCode, item.LastError, item.CreatedAt, item.NextAttemptAt, item.CompletedAt))
            .ToArrayAsync(ct);

    public async Task<WebhookMetricsSummary> GetMetricsAsync(Guid projectId, CancellationToken ct)
    {
        var rows = await db.WebhookDeliveries.AsNoTracking().Where(item => item.ProjectId == projectId)
            .GroupBy(item => item.Status).Select(group => new { Status = group.Key, Count = group.LongCount(), Attempts = group.Sum(item => (long)item.AttemptCount) }).ToArrayAsync(ct);
        return new WebhookMetricsSummary(
            rows.Where(item => item.Status == WebhookDeliveryStatus.Succeeded).Sum(item => item.Count),
            rows.Where(item => item.Status == WebhookDeliveryStatus.Failed).Sum(item => item.Count),
            rows.Where(item => item.Status == WebhookDeliveryStatus.Pending).Sum(item => item.Count),
            rows.Sum(item => item.Attempts));
    }

    public async Task CancelPendingForWebhookAsync(Guid webhookId, DateTimeOffset now, CancellationToken ct)
    {
        var outstanding = await db.WebhookDeliveries.Where(item => item.WebhookId == webhookId && (item.Status == WebhookDeliveryStatus.Pending || item.Status == WebhookDeliveryStatus.Failed)).ToArrayAsync(ct);
        foreach (var delivery in outstanding) delivery.Cancel(now);
    }

    public async Task<int> RetryFailedAsync(Guid projectId, DateTimeOffset now, CancellationToken ct)
    {
        var failed = await db.WebhookDeliveries.Where(item => item.ProjectId == projectId && item.Status == WebhookDeliveryStatus.Failed &&
            db.WebhookEndpoints.Any(webhook => webhook.Id == item.WebhookId && webhook.IsEnabled)).ToArrayAsync(ct);
        foreach (var delivery in failed) delivery.RetryManually(now);
        await db.SaveChangesAsync(ct);
        return failed.Length;
    }

    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
