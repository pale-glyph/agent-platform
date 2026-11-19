namespace AgentPlatform.Domain.Accounts;

public sealed class Account
{
    public const int MaximumClerkUserIdLength = 255;

    public Account(Guid id, string clerkUserId, DateTimeOffset createdAt, bool isAdmin = false)
    {
        Id = id;
        ClerkUserId = NormalizeClerkUserId(clerkUserId);
        CreatedAt = createdAt;
        LastSeenAt = createdAt;
        IsAdmin = isAdmin;
    }

    public Guid Id { get; }
    public string ClerkUserId { get; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset LastSeenAt { get; private set; }
    public bool IsAdmin { get; private set; }

    public void RecordActivity(DateTimeOffset occurredAt) => LastSeenAt = occurredAt;
    public void SetAdministrator(bool isAdmin) => IsAdmin = isAdmin;

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
