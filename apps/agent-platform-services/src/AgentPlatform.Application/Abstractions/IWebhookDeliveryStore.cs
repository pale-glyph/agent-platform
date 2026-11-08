using AgentPlatform.Domain.Webhooks;

namespace AgentPlatform.Application.Abstractions;

public interface IWebhookDeliveryStore
{
    Task<WebhookDelivery?> ClaimNextAsync(DateTimeOffset now, CancellationToken cancellationToken);
    Task MarkSucceededAsync(WebhookDelivery delivery, DateTimeOffset now, int statusCode, CancellationToken cancellationToken);
    Task MarkFailedAsync(WebhookDelivery delivery, DateTimeOffset now, string error, int? statusCode, bool retryable, CancellationToken cancellationToken);
}
