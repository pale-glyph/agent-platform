using System.Text.Json;
using AgentPlatform.Application.Abstractions;
using AgentPlatform.Domain.Webhooks;
using AgentPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AgentPlatform.Infrastructure.Webhooks;

public sealed class EfWebhookEventPublisher(PlatformDbContext db, TimeProvider clock) : IWebhookEventPublisher
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task EnqueueAsync(Guid projectId, string eventType, object payload, CancellationToken ct)
    {
        var subscribed = await db.WebhookEndpoints.Where(item => item.ProjectId == projectId && item.IsEnabled).ToArrayAsync(ct);
        var occurredAt = clock.GetUtcNow();
        foreach (var webhook in subscribed.Where(item => item.Events.Contains(eventType, StringComparer.Ordinal)))
        {
            var eventId = Guid.NewGuid();
            var body = JsonSerializer.Serialize(new
            {
                eventId,
                schemaVersion = "1.0",
                projectId,
                eventType,
                occurredAt,
                data = payload,
            }, JsonOptions);
            await db.WebhookDeliveries.AddAsync(new WebhookDelivery(Guid.NewGuid(), projectId, webhook.Id, webhook.Name, eventId, eventType, webhook.EndpointUrl, webhook.Secret, body, occurredAt), ct);
        }
    }
}
