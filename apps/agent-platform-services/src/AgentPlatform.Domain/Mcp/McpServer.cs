namespace AgentPlatform.Domain.Mcp;

public enum McpServerSecurity
{
    None,
    ApiKey,
}

public sealed class McpServer
{
    public const int MaximumNameLength = 200;
    public const int MaximumEndpointLength = 2_000;
    public const int MaximumApiKeyLength = 10_000;
    public const int MaximumCacheTtlSeconds = 86_400;

    public McpServer(Guid id, Guid projectId, string name, string endpoint, int cacheTtlSeconds, McpServerSecurity security, string? apiKey, DateTimeOffset createdAt)
    {
        Id = id;
        ProjectId = projectId;
        CreatedAt = createdAt;
        Update(name, endpoint, cacheTtlSeconds, security, apiKey);
    }

    public Guid Id { get; }
    public Guid ProjectId { get; }
    public string Name { get; private set; } = null!;
    public string Endpoint { get; private set; } = null!;
    public int CacheTtlSeconds { get; private set; }
    public McpServerSecurity Security { get; private set; }
    public string? ApiKey { get; private set; }
    public string? CachedToolDefinitions { get; private set; }
    public DateTimeOffset? CacheExpiresAt { get; private set; }
    public DateTimeOffset CreatedAt { get; }

    public void Update(string name, string endpoint, int cacheTtlSeconds, McpServerSecurity security, string? apiKey)
    {
        var normalizedEndpoint = Required(endpoint, MaximumEndpointLength, "Server endpoint");
        if (!Uri.TryCreate(normalizedEndpoint, UriKind.Absolute, out var endpointUri) || endpointUri.Scheme is not ("http" or "https"))
            throw new ArgumentException("Server endpoint must be an absolute HTTP or HTTPS URL.", nameof(endpoint));
        if (cacheTtlSeconds is < 1 or > MaximumCacheTtlSeconds)
            throw new ArgumentException($"Cache TTL must be between 1 and {MaximumCacheTtlSeconds} seconds.", nameof(cacheTtlSeconds));
        if (!Enum.IsDefined(security)) throw new ArgumentException("Security must be None or ApiKey.", nameof(security));

        Name = Required(name, MaximumNameLength, "Name");
        Endpoint = normalizedEndpoint;
        CacheTtlSeconds = cacheTtlSeconds;
        Security = security;
        ApiKey = security switch
        {
            McpServerSecurity.None => null,
            McpServerSecurity.ApiKey when apiKey is not null => Required(apiKey, MaximumApiKeyLength, "API key"),
            McpServerSecurity.ApiKey when string.IsNullOrEmpty(ApiKey) => throw new ArgumentException("API key is required when API key security is selected.", nameof(apiKey)),
            _ => ApiKey,
        };
        ClearToolDefinitionCache();
    }

    public bool HasValidToolDefinitionCache(DateTimeOffset now) => CachedToolDefinitions is not null && CacheExpiresAt > now;

    public void CacheToolDefinitions(string definitions, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(definitions)) throw new ArgumentException("Tool definitions are required.", nameof(definitions));
        CachedToolDefinitions = definitions;
        CacheExpiresAt = now.AddSeconds(CacheTtlSeconds);
    }

    public void ClearToolDefinitionCache()
    {
        CachedToolDefinitions = null;
        CacheExpiresAt = null;
    }

    private static string Required(string value, int maximumLength, string field)
    {
        var result = value?.Trim();
        if (string.IsNullOrWhiteSpace(result)) throw new ArgumentException($"{field} is required.", nameof(value));
        if (result.Length > maximumLength) throw new ArgumentException($"{field} must be {maximumLength} characters or fewer.", nameof(value));
        return result;
    }
}
