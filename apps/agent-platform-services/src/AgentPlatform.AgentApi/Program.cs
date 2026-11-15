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
builder.Services.AddOpenApi(options => options.AddDocumentTransformer((document, _, _) =>
{
    document.Info.Title = "Agent Platform Thread API";
    document.Info.Version = "v1";
    document.Info.Description = """
        Use this API to create and operate durable conversations with a configured platform agent.

        Start a thread with an initial user prompt, then poll its timeline in sequence-number order until the agent has produced the outcome your application needs. Use a `Queued` message to deliver a follow-up after the current run and a `Steering` message to request cooperative cancellation of an active run.

        All identifiers in the paths are UUIDs. Thread and timeline reads are scoped to the supplied project ID; a resource in a different project returns `404`.

        API-key authentication will be added to this API before it is exposed outside the platform network. It is not enforced in this local development build.
        """;
    return Task.CompletedTask;
}));
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

var agentThreads = app.MapGroup("/api/projects/{projectId:guid}/agent-threads")
    .WithTags("Agent threads")
    .WithDescription("Durable conversation lifecycle and timeline operations. A timeline entry is either a user/agent message or a tool call recorded during execution.");
agentThreads.MapGet("/", async (Guid projectId, AgentThreadService service, CancellationToken ct) =>
    Results.Ok(await service.ListAsync(projectId, ct)))
    .WithName("ListAgentThreads")
    .WithSummary("List a project's agent threads")
    .WithDescription("Returns every thread in the project, newest first. Filter the returned `agentId` client-side when an application is concerned with one agent.")
    .Produces<IReadOnlyList<AgentThreadSummary>>(StatusCodes.Status200OK);
agentThreads.MapGet("/{threadId:guid}", async (Guid projectId, Guid threadId, AgentThreadService service, CancellationToken ct) =>
{
    var thread = await service.FindAsync(projectId, threadId, ct);
    return thread is null ? Results.NotFound() : Results.Ok(thread);
})
    .WithName("GetAgentThread")
    .WithSummary("Get an agent thread")
    .WithDescription("Use this to check the current lifecycle status. A thread starts as `Running`; executions may later become `Completed`, `Failed`, or another terminal state.")
    .Produces<AgentThreadSummary>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status404NotFound);
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
})
    .WithName("ListAgentThreadTimeline")
    .WithSummary("Read a thread's ordered timeline")
    .WithDescription("Poll this endpoint after creating a thread or submitting a message. Set `afterSequence` to the previous response's `nextCursor` to read only later entries. `limit` defaults to 50 and must be from 1 through 100. A null `nextCursor` means the returned page reached the current end of the timeline.")
    .Produces<AgentThreadPage>(StatusCodes.Status200OK)
    .ProducesValidationProblem(StatusCodes.Status400BadRequest)
    .Produces(StatusCodes.Status404NotFound);
agentThreads.MapDelete("/{threadId:guid}", async (Guid projectId, Guid threadId, AgentThreadService service, CancellationToken ct) =>
    await service.DeleteAsync(projectId, threadId, ct) ? Results.NoContent() : Results.NotFound())
    .WithName("DeleteAgentThread")
    .WithSummary("Delete an agent thread")
    .WithDescription("Permanently deletes the thread and its execution history. This action cannot be undone.")
    .Produces(StatusCodes.Status204NoContent)
    .Produces(StatusCodes.Status404NotFound);
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
})
    .WithName("SubmitAgentThreadMessage")
    .WithSummary("Queue or steer a thread message")
    .WithDescription("Submit a non-empty `content` value of at most 100,000 characters. Set `delivery` to `Queued` for the next execution turn; at most one queued message may be pending, otherwise the API returns `409`. Set it to `Steering` to add a message immediately and request cooperative cancellation of a running execution. `Initial` is reserved for thread creation and returns `400` here.\n\nExample request:\n```json\n{\n  \"content\": \"Use the existing project configuration and create the release plan.\",\n  \"delivery\": \"Queued\"\n}\n```")
    .Accepts<SubmitAgentMessageRequest>("application/json")
    .Produces<SubmittedThreadMessage>(StatusCodes.Status202Accepted)
    .ProducesValidationProblem(StatusCodes.Status400BadRequest)
    .Produces(StatusCodes.Status404NotFound)
    .Produces(StatusCodes.Status409Conflict);

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
})
    .WithName("CreateAgentThread")
    .WithSummary("Create and start an agent thread")
    .WithDescription("Creates a durable thread for the specified agent, records `initialPrompt` as its first user message, and starts execution. Supply a non-empty prompt of at most 100,000 characters. Poll the returned thread's timeline to receive messages and tool activity.\n\nExample request:\n```json\n{\n  \"initialPrompt\": \"Review the deployment request and tell me what information is missing.\"\n}\n```")
    .Accepts<TriggerAgentThreadRequest>("application/json")
    .Produces<AgentThreadSummary>(StatusCodes.Status201Created)
    .ProducesValidationProblem(StatusCodes.Status400BadRequest)
    .Produces(StatusCodes.Status404NotFound);

app.Run();

public sealed record TriggerAgentThreadRequest(string? InitialPrompt);
public sealed record SubmitAgentMessageRequest(string? Content, AgentMessageDelivery Delivery);
