using AgentPlatform.Domain.Accounts;

namespace AgentPlatform.Application.Abstractions;

public interface IAccountRepository
{
    Task UpsertActivityAsync(Account account, CancellationToken cancellationToken);
}
