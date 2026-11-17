namespace AgentPlatform.Domain.Agents;

public sealed class AgentApiKey
{
    public const int MaximumNameLength = 200;
    public const int HashLength = 64;
    public const int PrefixLength = 16;

    public AgentApiKey(Guid id, Guid agentId, string name, string keyHash, string keyPrefix, DateTimeOffset createdAt)
    {
        Id = id;
        AgentId = agentId;
        Name = NormalizeName(name);
        KeyHash = RequireLength(keyHash, HashLength, nameof(keyHash));
        KeyPrefix = RequireLength(keyPrefix, PrefixLength, nameof(keyPrefix));
        CreatedAt = createdAt;
    }

    public Guid Id { get; }
    public Guid AgentId { get; }
    public string Name { get; }
    public string KeyHash { get; }
    public string KeyPrefix { get; }
    public DateTimeOffset CreatedAt { get; }

    private static string NormalizeName(string value)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized)) throw new ArgumentException("API key name is required.", nameof(value));
        if (normalized.Length > MaximumNameLength) throw new ArgumentException($"API key name must be {MaximumNameLength} characters or fewer.", nameof(value));
        return normalized;
    }

    private static string RequireLength(string value, int length, string parameterName)
    {
        if (value?.Length != length) throw new ArgumentException("The API key value is invalid.", parameterName);
        return value;
    }
}
