namespace AgentPlatform.Application.Projects;

public sealed record ProjectSummary(Guid Id, string Name, DateTimeOffset CreatedAt);
