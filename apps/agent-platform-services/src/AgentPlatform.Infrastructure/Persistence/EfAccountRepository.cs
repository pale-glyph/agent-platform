using AgentPlatform.Application.Abstractions;
using AgentPlatform.Domain.Accounts;
using Microsoft.EntityFrameworkCore;
namespace AgentPlatform.Infrastructure.Persistence;

public sealed class EfAccountRepository(PlatformDbContext dbContext) : IAccountRepository
{
    public Task UpsertActivityAsync(Account account, CancellationToken cancellationToken) =>
        dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO accounts (id, clerk_user_id, created_at, last_seen_at, is_admin)
            VALUES ({account.Id}, {account.ClerkUserId}, {account.CreatedAt}, {account.LastSeenAt}, {account.IsAdmin})
            ON CONFLICT (clerk_user_id) DO UPDATE
            SET last_seen_at = EXCLUDED.last_seen_at,
                is_admin = accounts.is_admin OR EXCLUDED.is_admin;
            """, cancellationToken);

    public Task<Account?> FindByClerkUserIdAsync(string clerkUserId, CancellationToken cancellationToken) =>
        dbContext.Accounts.SingleOrDefaultAsync(item => item.ClerkUserId == clerkUserId, cancellationToken);

    public async Task<IReadOnlyList<Account>> ListAsync(CancellationToken cancellationToken) =>
        await dbContext.Accounts.AsNoTracking().OrderBy(item => item.ClerkUserId).ToArrayAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}
