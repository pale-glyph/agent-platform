namespace AgentPlatform.Application.Abstractions;

public interface IWebhookEventPublisher
{
    Task EnqueueAsync(Guid projectId, string eventType, object payload, CancellationToken cancellationToken);
}
