using AgentPlatform.Application.Abstractions;
using AgentPlatform.Domain.Agents;
using System.Security.Cryptography;
using System.Text;

namespace AgentPlatform.Application.Agents;

public sealed record AgentApiKeySummary(Guid Id, string Name, string KeyPrefix, DateTimeOffset CreatedAt);
public sealed record CreatedAgentApiKey(Guid Id, string Name, string KeyPrefix, DateTimeOffset CreatedAt, string Secret);

public sealed class AgentApiKeyService(IAgentRepository agents, IAgentApiKeyRepository keys, TimeProvider timeProvider)
{
    public async Task<IReadOnlyList<AgentApiKeySummary>?> ListAsync(Guid projectId, Guid agentId, CancellationToken cancellationToken)
    {
        if (await agents.FindByIdAsync(projectId, agentId, cancellationToken) is null) return null;
        return (await keys.ListByAgentAsync(agentId, cancellationToken)).Select(ToSummary).ToArray();
    }

    public async Task<CreatedAgentApiKey?> CreateAsync(Guid projectId, Guid agentId, string name, CancellationToken cancellationToken)
    {
        if (await agents.FindByIdAsync(projectId, agentId, cancellationToken) is null) return null;
        var secret = $"apak_{Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant()}";
        var key = new AgentApiKey(Guid.NewGuid(), agentId, name, Hash(secret), secret[..AgentApiKey.PrefixLength], timeProvider.GetUtcNow());
        await keys.AddAsync(key, cancellationToken);
        await keys.SaveChangesAsync(cancellationToken);
        return new CreatedAgentApiKey(key.Id, key.Name, key.KeyPrefix, key.CreatedAt, secret);
    }

    public async Task<bool> DeleteAsync(Guid projectId, Guid agentId, Guid keyId, CancellationToken cancellationToken)
    {
        if (await agents.FindByIdAsync(projectId, agentId, cancellationToken) is null) return false;
        var key = await keys.FindByIdAsync(agentId, keyId, cancellationToken);
        if (key is null) return false;
        keys.Remove(key);
        await keys.SaveChangesAsync(cancellationToken);
        return true;
    }

    public static string Hash(string secret) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(secret))).ToLowerInvariant();
    private static AgentApiKeySummary ToSummary(AgentApiKey key) => new(key.Id, key.Name, key.KeyPrefix, key.CreatedAt);
}
