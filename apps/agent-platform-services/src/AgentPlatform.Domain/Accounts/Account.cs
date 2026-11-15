namespace AgentPlatform.Domain.Accounts;

public sealed class Account
{
    public const int MaximumClerkUserIdLength = 255;

    public Account(Guid id, string clerkUserId, DateTimeOffset createdAt)
    {
        Id = id;
        ClerkUserId = NormalizeClerkUserId(clerkUserId);
        CreatedAt = createdAt;
        LastSeenAt = createdAt;
    }

    public Guid Id { get; }
    public string ClerkUserId { get; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset LastSeenAt { get; private set; }

    public void RecordActivity(DateTimeOffset occurredAt) => LastSeenAt = occurredAt;

    private static string NormalizeClerkUserId(string clerkUserId)
    {
        var normalized = clerkUserId?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException("A Clerk user ID is required.", nameof(clerkUserId));
        }

        if (normalized.Length > MaximumClerkUserIdLength)
        {
            throw new ArgumentException($"A Clerk user ID must be {MaximumClerkUserIdLength} characters or fewer.", nameof(clerkUserId));
        }

        return normalized;
    }
}
