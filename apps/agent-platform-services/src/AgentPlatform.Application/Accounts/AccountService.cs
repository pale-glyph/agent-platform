using AgentPlatform.Application.Abstractions;
using AgentPlatform.Domain.Accounts;

namespace AgentPlatform.Application.Accounts;

public sealed record AccountSummary(string ClerkUserId, bool IsAdmin, DateTimeOffset CreatedAt, DateTimeOffset LastSeenAt);

public sealed class AccountService(IAccountRepository accounts, IAdministratorBootstrap bootstrap, TimeProvider timeProvider)
{
    public Task RecordAuthenticatedAccountAsync(string clerkUserId, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        return accounts.UpsertActivityAsync(new Account(Guid.NewGuid(), clerkUserId, now, bootstrap.IsBootstrapAdministrator(clerkUserId)), cancellationToken);
    }

    public async Task<bool> IsAdministratorAsync(string clerkUserId, CancellationToken cancellationToken) =>
        (await accounts.FindByClerkUserIdAsync(clerkUserId, cancellationToken))?.IsAdmin == true;

    public async Task<IReadOnlyList<AccountSummary>> ListAsync(CancellationToken cancellationToken) =>
        (await accounts.ListAsync(cancellationToken)).Select(Map).ToArray();

    public async Task<AccountSummary?> SetAdministratorAsync(string clerkUserId, bool isAdmin, CancellationToken cancellationToken)
    {
        var account = await accounts.FindByClerkUserIdAsync(clerkUserId, cancellationToken);
        if (account is null) return null;
        if (!isAdmin && bootstrap.IsBootstrapAdministrator(clerkUserId))
            throw new InvalidOperationException("A bootstrap administrator cannot be revoked while configured as a bootstrap administrator.");
        account.SetAdministrator(isAdmin);
        await accounts.SaveChangesAsync(cancellationToken);
        return Map(account);
    }

    private static AccountSummary Map(Account account) => new(account.ClerkUserId, account.IsAdmin, account.CreatedAt, account.LastSeenAt);
}
