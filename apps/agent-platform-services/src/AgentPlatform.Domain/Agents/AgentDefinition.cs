namespace AgentPlatform.Domain.Agents;

public sealed class AgentDefinition
{
    public const int MaximumNameLength = 200;
    public const int MaximumDescriptionLength = 2_000;
    public const int MaximumSystemPromptLength = 20_000;

    public AgentDefinition(Guid id, Guid projectId, string name, string description, string systemPrompt, DateTimeOffset createdAt)
    {
        Id = id;
        ProjectId = projectId;
        Name = Normalize(name, MaximumNameLength, "Agent name");
        Description = Normalize(description, MaximumDescriptionLength, "Agent description");
        SystemPrompt = Normalize(systemPrompt, MaximumSystemPromptLength, "System prompt");
        CreatedAt = createdAt;
    }

    public Guid Id { get; }

    public Guid ProjectId { get; }

    public string Name { get; private set; }
    public string Description { get; private set; }
    public string SystemPrompt { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public void Update(string name, string description, string systemPrompt)
    {
        Name = Normalize(name, MaximumNameLength, "Agent name");
        Description = Normalize(description, MaximumDescriptionLength, "Agent description");
        SystemPrompt = Normalize(systemPrompt, MaximumSystemPromptLength, "System prompt");
    }

    private static string Normalize(string value, int maximumLength, string fieldName)
    {
        var normalizedValue = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedValue)) throw new ArgumentException($"{fieldName} is required.", nameof(value));
        if (normalizedValue.Length > maximumLength) throw new ArgumentException($"{fieldName} must be {maximumLength} characters or fewer.", nameof(value));
        return normalizedValue;
    }
}
