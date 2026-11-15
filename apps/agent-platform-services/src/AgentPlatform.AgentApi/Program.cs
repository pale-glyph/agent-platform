using AgentPlatform.Application.Threads;
using AgentPlatform.Domain.Threads;
using AgentPlatform.Infrastructure;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Platform")
    ?? throw new InvalidOperationException("ConnectionStrings:Platform must be configured.");
builder.Services.AddInfrastructure(connectionString);
builder.Services.AddScoped<AgentThreadService>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapHealthChecks("/health");
app.MapOpenApi();
app.MapGet("/redoc", () => Results.Content("""
    <!doctype html>
    <html lang="en">
    <head>
      <meta charset="utf-8" />
      <meta name="viewport" content="width=device-width, initial-scale=1" />
      <title>Agent Platform thread API documentation</title>
    </head>
    <body>
      <redoc spec-url="/openapi/v1.json"></redoc>
      <script src="https://cdn.jsdelivr.net/npm/redoc@2.5.0/bundles/redoc.standalone.js"></script>
    </body>
    </html>
    """, "text/html"));

var agentThreads = app.MapGroup("/api/projects/{projectId:guid}/agent-threads");
agentThreads.MapGet("/", async (Guid projectId, AgentThreadService service, CancellationToken ct) =>
    Results.Ok(await service.ListAsync(projectId, ct)));
agentThreads.MapGet("/{threadId:guid}", async (Guid projectId, Guid threadId, AgentThreadService service, CancellationToken ct) =>
{
    var thread = await service.FindAsync(projectId, threadId, ct);
    return thread is null ? Results.NotFound() : Results.Ok(thread);
});
agentThreads.MapGet("/{threadId:guid}/timeline", async (Guid projectId, Guid threadId, long? afterSequence, int? limit, AgentThreadService service, CancellationToken ct) =>
{
    try
    {
        var page = await service.ListEntriesAsync(projectId, threadId, afterSequence, limit ?? 50, ct);
        return page is null ? Results.NotFound() : Results.Ok(page);
    }
    catch (ArgumentOutOfRangeException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["pagination"] = [exception.Message] });
    }
});
agentThreads.MapDelete("/{threadId:guid}", async (Guid projectId, Guid threadId, AgentThreadService service, CancellationToken ct) =>
    await service.DeleteAsync(projectId, threadId, ct) ? Results.NoContent() : Results.NotFound());
agentThreads.MapPost("/{threadId:guid}/messages", async (Guid projectId, Guid threadId, SubmitAgentMessageRequest request, AgentThreadService service, CancellationToken ct) =>
{
    try
    {
        var message = await service.SubmitMessageAsync(projectId, threadId, request.Content ?? string.Empty, request.Delivery, ct);
        return message is null ? Results.NotFound() : Results.Accepted($"/api/projects/{projectId}/agent-threads/{threadId}/timeline", message);
    }
    catch (InvalidOperationException exception)
    {
        return Results.Conflict(new { title = exception.Message });
    }
    catch (ArgumentException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["message"] = [exception.Message] });
    }
});

app.MapPost("/api/projects/{projectId:guid}/agents/{agentId:guid}/threads", async (Guid projectId, Guid agentId, TriggerAgentThreadRequest request, AgentThreadService service, CancellationToken ct) =>
{
    try
    {
        var thread = await service.TriggerAsync(projectId, agentId, request.InitialPrompt ?? string.Empty, ct);
        return thread is null ? Results.NotFound() : Results.Created($"/api/projects/{projectId}/agent-threads/{thread.Id}", thread);
    }
    catch (ArgumentException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["initialPrompt"] = [exception.Message] });
    }
});

app.Run();

public sealed record TriggerAgentThreadRequest(string? InitialPrompt);
public sealed record SubmitAgentMessageRequest(string? Content, AgentMessageDelivery Delivery);
