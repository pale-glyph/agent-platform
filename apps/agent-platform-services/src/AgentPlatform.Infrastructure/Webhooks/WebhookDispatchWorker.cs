using System.Security.Cryptography;
using System.Text;
using AgentPlatform.Application.Abstractions;
using AgentPlatform.Domain.Webhooks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AgentPlatform.Infrastructure.Webhooks;

public sealed class WebhookDispatchWorker(IServiceScopeFactory scopeFactory, IHttpClientFactory clients, TimeProvider clock, ILogger<WebhookDispatchWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Webhook dispatch worker started.");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var store = scope.ServiceProvider.GetRequiredService<IWebhookDeliveryStore>();
                var delivery = await store.ClaimNextAsync(clock.GetUtcNow(), stoppingToken);
                if (delivery is null)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                    continue;
                }

                await DispatchAsync(delivery, store, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Webhook dispatch iteration failed.");
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }
    }

    private async Task DispatchAsync(WebhookDelivery delivery, IWebhookDeliveryStore store, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, delivery.EndpointUrl)
        {
            Content = new StringContent(delivery.Payload, Encoding.UTF8, "application/json"),
        };
        request.Headers.TryAddWithoutValidation("X-Agent-Platform-Event-Id", delivery.EventId.ToString());
        request.Headers.TryAddWithoutValidation("X-Agent-Platform-Event-Type", delivery.EventType);
        var signature = Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(delivery.SigningSecret), Encoding.UTF8.GetBytes(delivery.Payload))).ToLowerInvariant();
        request.Headers.TryAddWithoutValidation("X-Agent-Platform-Signature", $"sha256={signature}");

        try
        {
            using var response = await clients.CreateClient("webhooks").SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            var now = clock.GetUtcNow();
            if (response.IsSuccessStatusCode)
            {
                await store.MarkSucceededAsync(delivery, now, (int)response.StatusCode, ct);
                logger.LogInformation("Webhook delivery {DeliveryId} completed with status {StatusCode}.", delivery.Id, (int)response.StatusCode);
                return;
            }

            var retryable = (int)response.StatusCode >= 500 || response.StatusCode is System.Net.HttpStatusCode.RequestTimeout or System.Net.HttpStatusCode.TooManyRequests;
            await store.MarkFailedAsync(delivery, now, $"Receiver returned HTTP {(int)response.StatusCode}.", (int)response.StatusCode, retryable, ct);
            logger.LogWarning("Webhook delivery {DeliveryId} returned status {StatusCode}; retryable: {Retryable}.", delivery.Id, (int)response.StatusCode, retryable);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            await store.MarkFailedAsync(delivery, clock.GetUtcNow(), exception is TaskCanceledException ? "Webhook request timed out." : "Webhook request failed.", null, true, ct);
            logger.LogWarning("Webhook delivery {DeliveryId} failed; it will be retried.", delivery.Id);
        }
    }
}
