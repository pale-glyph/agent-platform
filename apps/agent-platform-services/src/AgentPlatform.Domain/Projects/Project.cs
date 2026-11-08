namespace AgentPlatform.Domain.Projects;

public sealed class Project
{
    public const int MaximumNameLength = 200;

    public Project(Guid id, string name, DateTimeOffset createdAt)
    {
        Id = id;
        Name = NormalizeName(name);
        CreatedAt = createdAt;
    }

    public Guid Id { get; }

    public string Name { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public void Rename(string name) => Name = NormalizeName(name);

    private static string NormalizeName(string name)
    {
        var normalizedName = name?.Trim();

        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            throw new ArgumentException("Project name is required.", nameof(name));
        }

        if (normalizedName.Length > MaximumNameLength)
        {
            throw new ArgumentException($"Project name must be {MaximumNameLength} characters or fewer.", nameof(name));
        }

        return normalizedName;
    }
}
