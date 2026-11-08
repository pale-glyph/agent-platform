using AgentPlatform.Application.Agents;
using AgentPlatform.Application.Llm;
using AgentPlatform.Application.Projects;
using AgentPlatform.Application.Mcp;
using AgentPlatform.Application.Webhooks;
using AgentPlatform.Application.Instances;
using AgentPlatform.Application.Runtime;
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
builder.Services.AddScoped<WebhookService>();
builder.Services.AddScoped<AgentInstanceService>();
builder.Services.AddScoped<AgentRuntimeService>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddHealthChecks();
builder.Services.AddHostedService<AgentPlatform.Infrastructure.Webhooks.WebhookDispatchWorker>();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.MapHealthChecks("/health");
app.MapPlatformMcp();
app.MapPost("/webhooks/receive", async (HttpRequest request, CancellationToken ct) =>
{
    await request.Body.CopyToAsync(Stream.Null, ct);
    return Results.Ok();
});

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
        var agent = await service.CreateAsync(projectId, request.Name ?? string.Empty, request.Description ?? string.Empty, request.SystemPrompt ?? string.Empty, request.LlmConfigurationId, (request.McpToolGrants ?? []).Select(item => new McpToolGrantSummary(item.McpServerId, item.ToolName ?? string.Empty)).ToArray(), cancellationToken);
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
        var agent = await service.UpdateAsync(projectId, agentId, request.Name ?? string.Empty, request.Description ?? string.Empty, request.SystemPrompt ?? string.Empty, request.LlmConfigurationId, (request.McpToolGrants ?? []).Select(item => new McpToolGrantSummary(item.McpServerId, item.ToolName ?? string.Empty)).ToArray(), cancellationToken);
        return agent is null ? Results.NotFound() : Results.Ok(AgentResponse.FromSummary(agent));
    }
    catch (ArgumentException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["agent"] = [exception.Message] });
    }
});

agents.MapDelete("/{agentId:guid}", async (Guid projectId, Guid agentId, AgentService service, CancellationToken cancellationToken) =>
    await service.DeleteAsync(projectId, agentId, cancellationToken) ? Results.NoContent() : Results.NotFound());

agents.MapPost("/{agentId:guid}/instances", async (Guid projectId, Guid agentId, TriggerAgentInstanceRequest request, AgentInstanceService service, CancellationToken ct) =>
{
    try
    {
        var instance = await service.TriggerAsync(projectId, agentId, request.InitialPrompt ?? string.Empty, ct);
        return instance is null ? Results.NotFound() : Results.Created($"/api/projects/{projectId}/agent-instances/{instance.Id}", instance);
    }
    catch (ArgumentException exception) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["initialPrompt"] = [exception.Message] }); }
});

var agentInstances = projects.MapGroup("/{projectId:guid}/agent-instances");
agentInstances.MapGet("/", async (Guid projectId, AgentInstanceService service, CancellationToken ct) => Results.Ok(await service.ListAsync(projectId, ct)));
agentInstances.MapGet("/{instanceId:guid}/timeline", async (Guid projectId, Guid instanceId, long? afterSequence, int? limit, AgentInstanceService service, CancellationToken ct) =>
{
    try
    {
        var page = await service.ListEntriesAsync(projectId, instanceId, afterSequence, limit ?? 50, ct);
        return page is null ? Results.NotFound() : Results.Ok(page);
    }
    catch (ArgumentOutOfRangeException exception) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["pagination"] = [exception.Message] }); }
});
agentInstances.MapPost("/{instanceId:guid}/messages", async (Guid projectId, Guid instanceId, SubmitAgentMessageRequest request, AgentInstanceService service, CancellationToken ct) =>
{
    try
    {
        var message = await service.SubmitMessageAsync(projectId, instanceId, request.Content ?? string.Empty, request.Delivery, ct);
        return message is null ? Results.NotFound() : Results.Accepted($"/api/projects/{projectId}/agent-instances/{instanceId}/timeline", message);
    }
    catch (InvalidOperationException exception) { return Results.Conflict(new { title = exception.Message }); }
    catch (ArgumentException exception) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["message"] = [exception.Message] }); }
});
agentInstances.MapPost("/{instanceId:guid}/tool-calls", async (Guid projectId, Guid instanceId, RecordToolCallRequest request, AgentInstanceService service, CancellationToken ct) =>
{
    try
    {
        var toolCall = await service.RecordToolCallAsync(projectId, instanceId, request.ToolName ?? string.Empty, request.Arguments ?? string.Empty, request.Result, ct);
        return toolCall is null ? Results.NotFound() : Results.Created($"/api/projects/{projectId}/agent-instances/{instanceId}/timeline", toolCall);
    }
    catch (ArgumentException exception) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["toolCall"] = [exception.Message] }); }
});

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

var webhooks = projects.MapGroup("/{projectId:guid}/webhooks");
webhooks.MapGet("/", async (Guid projectId, WebhookService service, CancellationToken ct) =>
{
    var items = await service.ListAsync(projectId, ct);
    return items is null ? Results.NotFound() : Results.Ok(items);
});
webhooks.MapPost("/", async (Guid projectId, WebhookRequest request, WebhookService service, CancellationToken ct) =>
{
    try
    {
        var item = await service.CreateAsync(projectId, request.Name ?? "", request.EndpointUrl ?? "", request.Secret ?? "", request.Events ?? [], ct);
        return item is null ? Results.NotFound() : Results.Created($"/api/projects/{projectId}/webhooks/{item.Id}", item);
    }
    catch (ArgumentException exception) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["webhook"] = [exception.Message] }); }
});
webhooks.MapPut("/{webhookId:guid}", async (Guid projectId, Guid webhookId, WebhookRequest request, WebhookService service, CancellationToken ct) =>
{
    try
    {
        var item = await service.UpdateAsync(projectId, webhookId, request.Name ?? "", request.EndpointUrl ?? "", request.Secret, request.Events ?? [], request.Enabled ?? true, ct);
        return item is null ? Results.NotFound() : Results.Ok(item);
    }
    catch (ArgumentException exception) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["webhook"] = [exception.Message] }); }
});
webhooks.MapDelete("/{webhookId:guid}", async (Guid projectId, Guid webhookId, WebhookService service, CancellationToken ct) =>
    await service.DeleteAsync(projectId, webhookId, ct) ? Results.NoContent() : Results.NotFound());
webhooks.MapGet("/metrics", async (Guid projectId, WebhookService service, CancellationToken ct) =>
{
    var metrics = await service.GetMetricsAsync(projectId, ct);
    return metrics is null ? Results.NotFound() : Results.Ok(metrics);
});
webhooks.MapGet("/deliveries", async (Guid projectId, WebhookService service, CancellationToken ct) =>
{
    var deliveries = await service.ListDeliveriesAsync(projectId, ct);
    return deliveries is null ? Results.NotFound() : Results.Ok(deliveries);
});
webhooks.MapPost("/deliveries/retry-failed", async (Guid projectId, WebhookService service, CancellationToken ct) =>
{
    var retried = await service.RetryFailedAsync(projectId, ct);
    return retried is null ? Results.NotFound() : Results.Ok(new { retried });
});

app.Run();

public sealed record ProjectRequest(string? Name);
public sealed record AgentRequest(string? Name, string? Description, string? SystemPrompt, Guid? LlmConfigurationId, McpToolGrantRequest[]? McpToolGrants);
public sealed record McpToolGrantRequest(Guid McpServerId, string? ToolName);
public sealed record LlmConfigurationRequest(string? Name, string? ProviderUrl, string? ModelName, string? ApiKey);
public sealed record McpServerRequest(string? Name, string? Endpoint, int CacheTtlSeconds, McpServerSecurity Security, string? ApiKey);
public sealed record WebhookRequest(string? Name, string? EndpointUrl, string? Secret, string[]? Events, bool? Enabled);
public sealed record TriggerAgentInstanceRequest(string? InitialPrompt);
public sealed record SubmitAgentMessageRequest(string? Content, AgentPlatform.Domain.Instances.AgentMessageDelivery Delivery);
public sealed record RecordToolCallRequest(string? ToolName, string? Arguments, string? Result);
public sealed record ProjectResponse(Guid Id, string Name, DateTimeOffset CreatedAt)
{
    public static ProjectResponse FromSummary(ProjectSummary summary) => new(summary.Id, summary.Name, summary.CreatedAt);
}
public sealed record AgentResponse(Guid Id, Guid ProjectId, string Name, string Description, string SystemPrompt, Guid? LlmConfigurationId, IReadOnlyList<McpToolGrantSummary> McpToolGrants, DateTimeOffset CreatedAt)
{
    public static AgentResponse FromSummary(AgentSummary summary) => new(summary.Id, summary.ProjectId, summary.Name, summary.Description, summary.SystemPrompt, summary.LlmConfigurationId, summary.McpToolGrants, summary.CreatedAt);
}
public sealed record McpServerResponse(Guid Id, Guid ProjectId, string Name, string Endpoint, int CacheTtlSeconds, McpServerSecurity Security, DateTimeOffset? CacheExpiresAt, DateTimeOffset CreatedAt)
{
    public static McpServerResponse FromSummary(McpServerSummary summary) => new(summary.Id, summary.ProjectId, summary.Name, summary.Endpoint, summary.CacheTtlSeconds, summary.Security, summary.CacheExpiresAt, summary.CreatedAt);
}
