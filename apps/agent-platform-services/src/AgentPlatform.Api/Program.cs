using AgentPlatform.Application.Projects;
using AgentPlatform.Infrastructure;
using AgentPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Platform")
    ?? throw new InvalidOperationException("ConnectionStrings:Platform must be configured.");
builder.Services.AddInfrastructure(connectionString);
builder.Services.AddScoped<ProjectService>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddHealthChecks();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.MapHealthChecks("/health");

var projects = app.MapGroup("/api/projects");
projects.MapGet("/", async (ProjectService service, CancellationToken cancellationToken) =>
    Results.Ok((await service.ListAsync(cancellationToken)).Select(ProjectResponse.FromSummary)));

projects.MapGet("/{projectId:guid}", async (Guid projectId, ProjectService service, CancellationToken cancellationToken) =>
{
    var project = await service.FindAsync(projectId, cancellationToken);
    return project is null ? Results.NotFound() : Results.Ok(ProjectResponse.FromSummary(project));
});

projects.MapPost("/", async (ProjectRequest request, ProjectService service, CancellationToken cancellationToken) =>
{
    try
    {
        var project = await service.CreateAsync(request.Name ?? string.Empty, cancellationToken);
        return Results.Created($"/api/projects/{project.Id}", ProjectResponse.FromSummary(project));
    }
    catch (ArgumentException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["name"] = [exception.Message] });
    }
});

projects.MapPut("/{projectId:guid}", async (Guid projectId, ProjectRequest request, ProjectService service, CancellationToken cancellationToken) =>
{
    try
    {
        var project = await service.UpdateAsync(projectId, request.Name ?? string.Empty, cancellationToken);
        return project is null ? Results.NotFound() : Results.Ok(ProjectResponse.FromSummary(project));
    }
    catch (ArgumentException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["name"] = [exception.Message] });
    }
});

projects.MapDelete("/{projectId:guid}", async (Guid projectId, ProjectService service, CancellationToken cancellationToken) =>
    await service.DeleteAsync(projectId, cancellationToken) ? Results.NoContent() : Results.NotFound());

app.Run();

public sealed record ProjectRequest(string? Name);
public sealed record ProjectResponse(Guid Id, string Name, DateTimeOffset CreatedAt)
{
    public static ProjectResponse FromSummary(ProjectSummary summary) => new(summary.Id, summary.Name, summary.CreatedAt);
}
