using System.Data;
using AgentPlatform.Application.Abstractions;
using AgentPlatform.Domain.Webhooks;
using AgentPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AgentPlatform.Infrastructure.Webhooks;

public sealed class EfWebhookDeliveryStore(PlatformDbContext db) : IWebhookDeliveryStore
{
    public async Task<WebhookDelivery?> ClaimNextAsync(DateTimeOffset now, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        var dueDeliveries = await db.WebhookDeliveries.FromSqlInterpolated($"""
            SELECT d.* FROM webhook_deliveries AS d
            INNER JOIN webhook_endpoints AS w ON w.id = d.webhook_id
            WHERE d.status = 'Pending'
              AND d.next_attempt_at <= {now}
              AND (d.claimed_until IS NULL OR d.claimed_until <= {now})
              AND w.is_enabled = TRUE
              AND (w.circuit_open_until IS NULL OR w.circuit_open_until <= {now})
            ORDER BY d.next_attempt_at, d.created_at
            LIMIT 1
            FOR UPDATE OF d SKIP LOCKED
            """).ToListAsync(ct);
        var delivery = dueDeliveries.FirstOrDefault();
        if (delivery is null)
        {
            await transaction.CommitAsync(ct);
            return null;
        }

        delivery.Claim(now);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return delivery;
    }

    public async Task MarkSucceededAsync(WebhookDelivery delivery, DateTimeOffset now, int statusCode, CancellationToken ct)
    {
        delivery.Succeed(now, statusCode);
        var webhook = await db.WebhookEndpoints.SingleOrDefaultAsync(item => item.Id == delivery.WebhookId, ct);
        webhook?.RecordSuccess();
        await db.SaveChangesAsync(ct);
    }

    public async Task MarkFailedAsync(WebhookDelivery delivery, DateTimeOffset now, string error, int? statusCode, bool retryable, CancellationToken ct)
    {
        delivery.Fail(now, error, statusCode, retryable);
        var webhook = await db.WebhookEndpoints.SingleOrDefaultAsync(item => item.Id == delivery.WebhookId, ct);
        webhook?.RecordFailure(now);
        await db.SaveChangesAsync(ct);
    }
}
