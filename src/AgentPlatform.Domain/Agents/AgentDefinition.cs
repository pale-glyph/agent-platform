namespace AgentPlatform.Domain.Agents;

public sealed class AgentDefinition
{
    public AgentDefinition(Guid id, Guid projectId, string name, DateTimeOffset createdAt)
    {
        Id = id;
        ProjectId = projectId;
        Name = name;
        CreatedAt = createdAt;
    }

    public Guid Id { get; }

    public Guid ProjectId { get; }

    public string Name { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public void Rename(string name) => Name = name;
}
