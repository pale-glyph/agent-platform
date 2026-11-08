using AgentPlatform.Application.Agents;
using AgentPlatform.Application.Llm;
using AgentPlatform.Application.Projects;
using AgentPlatform.Application.Mcp;
using AgentPlatform.Domain.Mcp;
using AgentPlatform.Api.Mcp;
using AgentPlatform.Infrastructure;
using AgentPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Platform")
    ?? throw new InvalidOperationException("ConnectionStrings:Platform must be configured.");
builder.Services.AddInfrastructure(connectionString);
builder.Services.AddScoped<ProjectService>();
builder.Services.AddScoped<AgentService>();
builder.Services.AddScoped<LlmConfigurationService>();
builder.Services.AddScoped<McpServerService>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddHealthChecks();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.MapHealthChecks("/health");
app.MapPlatformMcp();

var llmConfigurations = app.MapGroup("/api/llm-configurations");
llmConfigurations.MapGet("/", async (LlmConfigurationService service, CancellationToken ct) => Results.Ok(await service.ListAsync(ct)));
llmConfigurations.MapPost("/", async (LlmConfigurationRequest request, LlmConfigurationService service, CancellationToken ct) => { try { var item = await service.CreateAsync(request.Name ?? "", request.ProviderUrl ?? "", request.ModelName ?? "", request.ApiKey ?? "", ct); return Results.Created($"/api/llm-configurations/{item.Id}", item); } catch (ArgumentException exception) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["configuration"] = [exception.Message] }); } });
llmConfigurations.MapPut("/{id:guid}", async (Guid id, LlmConfigurationRequest request, LlmConfigurationService service, CancellationToken ct) => { try { var item = await service.UpdateAsync(id, request.Name ?? "", request.ProviderUrl ?? "", request.ModelName ?? "", request.ApiKey, ct); return item is null ? Results.NotFound() : Results.Ok(item); } catch (ArgumentException exception) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["configuration"] = [exception.Message] }); } });
llmConfigurations.MapDelete("/{id:guid}", async (Guid id, LlmConfigurationService service, CancellationToken ct) => await service.DeleteAsync(id, ct) ? Results.NoContent() : Results.NotFound());

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

var agents = projects.MapGroup("/{projectId:guid}/agents");
agents.MapGet("/", async (Guid projectId, AgentService service, CancellationToken cancellationToken) =>
{
    var items = await service.ListAsync(projectId, cancellationToken);
    return items is null ? Results.NotFound() : Results.Ok(items.Select(AgentResponse.FromSummary));
});

agents.MapPost("/", async (Guid projectId, AgentRequest request, AgentService service, CancellationToken cancellationToken) =>
{
    try
    {
        var agent = await service.CreateAsync(projectId, request.Name ?? string.Empty, request.Description ?? string.Empty, request.SystemPrompt ?? string.Empty, cancellationToken);
        return agent is null ? Results.NotFound() : Results.Created($"/api/projects/{projectId}/agents/{agent.Id}", AgentResponse.FromSummary(agent));
    }
    catch (ArgumentException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["agent"] = [exception.Message] });
    }
});

agents.MapPut("/{agentId:guid}", async (Guid projectId, Guid agentId, AgentRequest request, AgentService service, CancellationToken cancellationToken) =>
{
    try
    {
        var agent = await service.UpdateAsync(projectId, agentId, request.Name ?? string.Empty, request.Description ?? string.Empty, request.SystemPrompt ?? string.Empty, cancellationToken);
        return agent is null ? Results.NotFound() : Results.Ok(AgentResponse.FromSummary(agent));
    }
    catch (ArgumentException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["agent"] = [exception.Message] });
    }
});

agents.MapDelete("/{agentId:guid}", async (Guid projectId, Guid agentId, AgentService service, CancellationToken cancellationToken) =>
    await service.DeleteAsync(projectId, agentId, cancellationToken) ? Results.NoContent() : Results.NotFound());

var mcpServers = projects.MapGroup("/{projectId:guid}/mcp-servers");
mcpServers.MapGet("/", async (Guid projectId, McpServerService service, CancellationToken ct) =>
{
    var items = await service.ListAsync(projectId, ct);
    return items is null ? Results.NotFound() : Results.Ok(items.Select(McpServerResponse.FromSummary));
});
mcpServers.MapPost("/", async (Guid projectId, McpServerRequest request, McpServerService service, CancellationToken ct) =>
{
    try
    {
        var item = await service.CreateAsync(projectId, request.Name ?? "", request.Endpoint ?? "", request.CacheTtlSeconds, request.Security, request.ApiKey, ct);
        return item is null ? Results.NotFound() : Results.Created($"/api/projects/{projectId}/mcp-servers/{item.Id}", McpServerResponse.FromSummary(item));
    }
    catch (ArgumentException exception) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["mcpServer"] = [exception.Message] }); }
});
mcpServers.MapPut("/{serverId:guid}", async (Guid projectId, Guid serverId, McpServerRequest request, McpServerService service, CancellationToken ct) =>
{
    try
    {
        var item = await service.UpdateAsync(projectId, serverId, request.Name ?? "", request.Endpoint ?? "", request.CacheTtlSeconds, request.Security, request.ApiKey, ct);
        return item is null ? Results.NotFound() : Results.Ok(McpServerResponse.FromSummary(item));
    }
    catch (ArgumentException exception) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["mcpServer"] = [exception.Message] }); }
});
mcpServers.MapDelete("/{serverId:guid}", async (Guid projectId, Guid serverId, McpServerService service, CancellationToken ct) =>
    await service.DeleteAsync(projectId, serverId, ct) ? Results.NoContent() : Results.NotFound());
mcpServers.MapPost("/{serverId:guid}/cache/clear", async (Guid projectId, Guid serverId, McpServerService service, CancellationToken ct) =>
    await service.ClearCacheAsync(projectId, serverId, ct) ? Results.NoContent() : Results.NotFound());
mcpServers.MapGet("/{serverId:guid}/tools", async (Guid projectId, Guid serverId, McpServerService service, CancellationToken ct) =>
{
    try
    {
        var definitions = await service.GetToolDefinitionsAsync(projectId, serverId, ct);
        return definitions is null ? Results.NotFound() : Results.Content(definitions.ToolsJson, "application/json");
    }
    catch (HttpRequestException) { return Results.Problem(title: "The MCP server could not be reached.", statusCode: StatusCodes.Status502BadGateway); }
    catch (InvalidOperationException) { return Results.Problem(title: "The MCP server returned an invalid tool definition response.", statusCode: StatusCodes.Status502BadGateway); }
});

app.Run();

public sealed record ProjectRequest(string? Name);
public sealed record AgentRequest(string? Name, string? Description, string? SystemPrompt);
public sealed record LlmConfigurationRequest(string? Name, string? ProviderUrl, string? ModelName, string? ApiKey);
public sealed record McpServerRequest(string? Name, string? Endpoint, int CacheTtlSeconds, McpServerSecurity Security, string? ApiKey);
public sealed record ProjectResponse(Guid Id, string Name, DateTimeOffset CreatedAt)
{
    public static ProjectResponse FromSummary(ProjectSummary summary) => new(summary.Id, summary.Name, summary.CreatedAt);
}
public sealed record AgentResponse(Guid Id, Guid ProjectId, string Name, string Description, string SystemPrompt, DateTimeOffset CreatedAt)
{
    public static AgentResponse FromSummary(AgentSummary summary) => new(summary.Id, summary.ProjectId, summary.Name, summary.Description, summary.SystemPrompt, summary.CreatedAt);
}
public sealed record McpServerResponse(Guid Id, Guid ProjectId, string Name, string Endpoint, int CacheTtlSeconds, McpServerSecurity Security, DateTimeOffset? CacheExpiresAt, DateTimeOffset CreatedAt)
{
    public static McpServerResponse FromSummary(McpServerSummary summary) => new(summary.Id, summary.ProjectId, summary.Name, summary.Endpoint, summary.CacheTtlSeconds, summary.Security, summary.CacheExpiresAt, summary.CreatedAt);
}
