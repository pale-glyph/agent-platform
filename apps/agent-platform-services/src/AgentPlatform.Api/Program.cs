using AgentPlatform.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure();
builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapHealthChecks("/health");
app.MapGet("/api/projects", () => Results.Ok(Array.Empty<ProjectResponse>()));

app.Run();

public sealed record ProjectResponse(Guid Id, string Name, DateTimeOffset CreatedAt);
