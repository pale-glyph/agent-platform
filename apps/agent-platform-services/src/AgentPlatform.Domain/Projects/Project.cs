namespace AgentPlatform.Domain.Projects;

public sealed class Project
{
    public Project(Guid id, string name, DateTimeOffset createdAt)
    {
        Id = id;
        Name = name;
        CreatedAt = createdAt;
    }

    public Guid Id { get; }

    public string Name { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public void Rename(string name) => Name = name;
}
