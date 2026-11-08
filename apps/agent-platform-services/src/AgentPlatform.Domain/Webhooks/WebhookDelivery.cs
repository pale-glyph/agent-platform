namespace AgentPlatform.Domain.Webhooks;

public enum WebhookDeliveryStatus
{
    Pending,
    Succeeded,
    Failed,
    Cancelled,
}

public sealed class WebhookDelivery
{
    private WebhookDelivery() { }

    public WebhookDelivery(Guid id, Guid projectId, Guid webhookId, string webhookName, Guid eventId, string eventType, string endpointUrl, string signingSecret, string payload, DateTimeOffset createdAt)
    {
        Id = id;
        ProjectId = projectId;
        WebhookId = webhookId;
        WebhookName = webhookName;
        EventId = eventId;
        EventType = eventType;
        EndpointUrl = endpointUrl;
        SigningSecret = signingSecret;
        Payload = payload;
        Status = WebhookDeliveryStatus.Pending;
        NextAttemptAt = createdAt;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid WebhookId { get; private set; }
    public string WebhookName { get; private set; } = null!;
    public Guid EventId { get; private set; }
    public string EventType { get; private set; } = null!;
    public string EndpointUrl { get; private set; } = null!;
    public string SigningSecret { get; private set; } = null!;
    public string Payload { get; private set; } = null!;
    public WebhookDeliveryStatus Status { get; private set; }
    public int AttemptCount { get; private set; }
    public int RetryAttemptCount { get; private set; }
    public DateTimeOffset NextAttemptAt { get; private set; }
    public DateTimeOffset? ClaimedUntil { get; private set; }
    public int? LastStatusCode { get; private set; }
    public string? LastError { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    public void Claim(DateTimeOffset now) { AttemptCount++; RetryAttemptCount++; ClaimedUntil = now.AddMinutes(2); }

    public void Succeed(DateTimeOffset now, int statusCode)
    {
        Status = WebhookDeliveryStatus.Succeeded;
        LastStatusCode = statusCode;
        LastError = null;
        ClaimedUntil = null;
        CompletedAt = now;
    }

    public void Fail(DateTimeOffset now, string error, int? statusCode, bool retryable)
    {
        if (Status == WebhookDeliveryStatus.Cancelled)
        {
            ClaimedUntil = null;
            return;
        }

        LastStatusCode = statusCode;
        LastError = error.Length > 2_000 ? error[..2_000] : error;
        ClaimedUntil = null;
        if (!retryable || RetryAttemptCount >= 10)
        {
            Status = WebhookDeliveryStatus.Failed;
            CompletedAt = now;
            return;
        }

        var delaySeconds = Math.Min(3_600, 5 * Math.Pow(2, Math.Min(RetryAttemptCount - 1, 10)));
        NextAttemptAt = now.AddSeconds(delaySeconds);
        Status = WebhookDeliveryStatus.Pending;
    }

    public void RetryManually(DateTimeOffset now)
    {
        if (Status != WebhookDeliveryStatus.Failed) return;
        Status = WebhookDeliveryStatus.Pending;
        RetryAttemptCount = 0;
        NextAttemptAt = now;
        ClaimedUntil = null;
        LastStatusCode = null;
        LastError = null;
        CompletedAt = null;
    }

    public void Cancel(DateTimeOffset now)
    {
        if (Status is not (WebhookDeliveryStatus.Pending or WebhookDeliveryStatus.Failed)) return;
        Status = WebhookDeliveryStatus.Cancelled;
        ClaimedUntil = null;
        CompletedAt = now;
    }
}
