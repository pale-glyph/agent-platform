using AgentPlatform.Domain.Accounts;

namespace AgentPlatform.Application.Abstractions;

public interface IAccountRepository
{
    Task UpsertActivityAsync(Account account, CancellationToken cancellationToken);
    Task<Account?> FindByClerkUserIdAsync(string clerkUserId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Account>> ListAsync(CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
