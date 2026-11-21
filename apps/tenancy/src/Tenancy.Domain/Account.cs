namespace Tenancy.Domain;

public sealed class Account
{
    public Account(Guid id, string clerkUserId, bool isPlatformAdministrator = false)
    {
        if (id == Guid.Empty) throw new ArgumentException("Account ID is required.", nameof(id));
        var subject = clerkUserId?.Trim();
        if (string.IsNullOrEmpty(subject) || subject.Length > 255)
            throw new ArgumentException("Clerk user ID must contain 1 to 255 characters.", nameof(clerkUserId));
        Id = id;
        ClerkUserId = subject;
        IsPlatformAdministrator = isPlatformAdministrator;
    }

    public Guid Id { get; }
    public string ClerkUserId { get; }
    public AccountStatus Status { get; private set; } = AccountStatus.Active;
    public bool IsPlatformAdministrator { get; private set; }

    public void SetAdministrator(bool value, int activeAdministratorCount)
    {
        if (Status != AccountStatus.Active) throw new InvalidOperationException("Account is not active.");
        if (!value && IsPlatformAdministrator && activeAdministratorCount <= 1)
            throw new InvalidOperationException("The last active administrator cannot be removed.");
        IsPlatformAdministrator = value;
    }

    // The caller must coordinate owned organisations and memberships in the same transaction.
    public void Deactivate(AccountStatus status, int activeAdministratorCount)
    {
        if (status == AccountStatus.Active) throw new ArgumentException("A non-active status is required.", nameof(status));
        if (Status == AccountStatus.Deleted && status != AccountStatus.Deleted)
            throw new InvalidOperationException("A deleted account cannot be revived.");
        if (Status == AccountStatus.Active && IsPlatformAdministrator && activeAdministratorCount <= 1)
            throw new InvalidOperationException("The last active administrator cannot be removed.");
        Status = status;
    }
}

public enum AccountStatus { Active, Deleted, Blocked }
