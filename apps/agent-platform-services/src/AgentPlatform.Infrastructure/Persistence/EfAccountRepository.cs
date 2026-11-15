using AgentPlatform.Application.Abstractions;
using AgentPlatform.Domain.Accounts;
using Microsoft.EntityFrameworkCore;
namespace AgentPlatform.Infrastructure.Persistence;

public sealed class EfAccountRepository(PlatformDbContext dbContext) : IAccountRepository
{
    public Task UpsertActivityAsync(Account account, CancellationToken cancellationToken) =>
        dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO accounts (id, clerk_user_id, created_at, last_seen_at)
            VALUES ({account.Id}, {account.ClerkUserId}, {account.CreatedAt}, {account.LastSeenAt})
            ON CONFLICT (clerk_user_id) DO UPDATE
            SET last_seen_at = EXCLUDED.last_seen_at;
            """, cancellationToken);
}
