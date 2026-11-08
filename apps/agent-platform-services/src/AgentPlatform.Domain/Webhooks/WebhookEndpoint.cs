using System.Text.Json;

namespace AgentPlatform.Domain.Webhooks;

public sealed class WebhookEndpoint
{
    public const int MaximumNameLength = 200;
    public const int MaximumUrlLength = 2_000;
    public const int MinimumSecretLength = 32;
    public const int MaximumSecretLength = 1_000;

    private WebhookEndpoint() { }

    public WebhookEndpoint(Guid id, Guid projectId, string name, string endpointUrl, string secret, IEnumerable<string> events, DateTimeOffset createdAt)
    {
        Id = id;
        ProjectId = projectId;
        CreatedAt = createdAt;
        IsEnabled = true;
        Update(name, endpointUrl, secret, events);
    }

    public Guid Id { get; private set; }
    public Guid ProjectId { get; private set; }
    public string Name { get; private set; } = null!;
    public string EndpointUrl { get; private set; } = null!;
    public string Secret { get; private set; } = null!;
    public string EventsJson { get; private set; } = "[]";
    public bool IsEnabled { get; private set; }
    public int ConsecutiveFailures { get; private set; }
    public DateTimeOffset? CircuitOpenUntil { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public IReadOnlyList<string> Events => JsonSerializer.Deserialize<string[]>(EventsJson) ?? [];

    public void Update(string name, string endpointUrl, string? secret, IEnumerable<string> events)
    {
        Name = Required(name, MaximumNameLength, "Webhook name");
        EndpointUrl = Required(endpointUrl, MaximumUrlLength, "Webhook URL");
        if (!Uri.TryCreate(EndpointUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps && !(uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback)))
            throw new ArgumentException("Webhook URL must use HTTPS. HTTP is allowed only for a loopback address.", nameof(endpointUrl));

        var normalizedEvents = (events ?? []).Select(item => item?.Trim() ?? "").Where(item => item.Length > 0).Distinct(StringComparer.Ordinal).Order().ToArray();
        if (normalizedEvents.Length == 0) throw new ArgumentException("Select at least one event.", nameof(events));
        var unknownEvent = normalizedEvents.FirstOrDefault(item => !WebhookEventTypes.All.Contains(item));
        if (unknownEvent is not null) throw new ArgumentException($"Unsupported webhook event '{unknownEvent}'.", nameof(events));

        if (!string.IsNullOrWhiteSpace(secret))
        {
            var normalizedSecret = secret.Trim();
            if (normalizedSecret.Length < MinimumSecretLength || normalizedSecret.Length > MaximumSecretLength)
                throw new ArgumentException($"Webhook secret must be between {MinimumSecretLength} and {MaximumSecretLength} characters.", nameof(secret));
            Secret = normalizedSecret;
        }
        else if (string.IsNullOrEmpty(Secret))
        {
            throw new ArgumentException("Webhook signing secret is required.", nameof(secret));
        }

        EventsJson = JsonSerializer.Serialize(normalizedEvents);
        ResetCircuit();
    }

    public void Disable() => IsEnabled = false;
    public void Enable() => IsEnabled = true;

    public void RecordSuccess()
    {
        ConsecutiveFailures = 0;
        CircuitOpenUntil = null;
    }

    public void RecordFailure(DateTimeOffset now)
    {
        ConsecutiveFailures++;
        if (ConsecutiveFailures >= 5)
        {
            CircuitOpenUntil = now.AddMinutes(1);
            ConsecutiveFailures = 0;
        }
    }

    private void ResetCircuit()
    {
        ConsecutiveFailures = 0;
        CircuitOpenUntil = null;
    }

    private static string Required(string value, int maximumLength, string field)
    {
        var result = value?.Trim();
        if (string.IsNullOrWhiteSpace(result)) throw new ArgumentException($"{field} is required.", nameof(value));
        if (result.Length > maximumLength) throw new ArgumentException($"{field} must be {maximumLength} characters or fewer.", nameof(value));
        return result;
    }
}
