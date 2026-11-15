using AgentPlatform.Application.Abstractions;
using AgentPlatform.Domain.Accounts;

namespace AgentPlatform.Application.Accounts;

public sealed class AccountService(IAccountRepository accounts, TimeProvider timeProvider)
{
    public Task RecordAuthenticatedAccountAsync(string clerkUserId, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        return accounts.UpsertActivityAsync(new Account(Guid.NewGuid(), clerkUserId, now), cancellationToken);
    }
}
