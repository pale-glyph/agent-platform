namespace AgentPlatform.Domain.Llm;

public sealed class LlmConfiguration
{
    public const int MaximumNameLength = 200;
    public const int MaximumProviderUrlLength = 2_000;
    public const int MaximumModelNameLength = 500;
    public const int MaximumApiKeyLength = 10_000;

    public LlmConfiguration(Guid id, string name, string providerUrl, string modelName, string apiKey, DateTimeOffset createdAt)
    {
        Id = id;
        CreatedAt = createdAt;
        Update(name, providerUrl, modelName, apiKey);
    }

    public Guid Id { get; }
    public string Name { get; private set; } = null!;
    public string ProviderUrl { get; private set; } = null!;
    public string ModelName { get; private set; } = null!;
    public string ApiKey { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; }

    public void Update(string name, string providerUrl, string modelName, string? apiKey)
    {
        Name = Required(name, MaximumNameLength, "Name");
        ProviderUrl = Required(providerUrl, MaximumProviderUrlLength, "Provider URL");
        if (!Uri.TryCreate(ProviderUrl, UriKind.Absolute, out _)) throw new ArgumentException("Provider URL must be an absolute URL.", nameof(providerUrl));
        ModelName = Required(modelName, MaximumModelNameLength, "Model name");
        if (apiKey is not null) ApiKey = Required(apiKey, MaximumApiKeyLength, "API key");
        else if (string.IsNullOrEmpty(ApiKey)) throw new ArgumentException("API key is required.", nameof(apiKey));
    }

    private static string Required(string value, int maximumLength, string field) { var result = value?.Trim(); if (string.IsNullOrWhiteSpace(result)) throw new ArgumentException($"{field} is required.", nameof(value)); if (result.Length > maximumLength) throw new ArgumentException($"{field} must be {maximumLength} characters or fewer.", nameof(value)); return result; }
}
