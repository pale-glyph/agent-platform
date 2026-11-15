using AgentPlatform.Application.Threads;
using AgentPlatform.Domain.Threads;
using AgentPlatform.Infrastructure;
using AgentPlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using System.Security.Claims;

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

        ## Receive results through webhooks

        Webhooks, rather than timeline polling, are the primary notification mechanism. Before an application starts threads, a human administrator must configure a webhook for its project in the administration UI: **Projects → select project → Webhooks → New webhook**. Set the application's HTTPS receiver URL and signing secret, then subscribe to `agent.message`. Subscribe to `agent.tool_call` only when the application needs tool activity as well.

        The platform sends a JSON `POST` with these headers:

        ```text
        X-Agent-Platform-Event-Id: <UUID>
        X-Agent-Platform-Event-Type: agent.message | agent.tool_call
        X-Agent-Platform-Signature: sha256=<lowercase hexadecimal HMAC-SHA-256>
        ```

        Verify the signature against the exact UTF-8 request body with the webhook signing secret before parsing the payload, using a constant-time comparison. The payload has `eventId`, `schemaVersion` (`"1.0"`), `projectId`, `eventType`, `occurredAt`, and `data`. For `agent.message`, `data` is a timeline entry; an agent response has `type: "Message"`, `role: "Agent"`, and `messageStatus: "Delivered"`.

        ```json
        {
          "eventId": "2f0d9d44-4a53-47e2-bf36-27f49dd8e31a",
          "schemaVersion": "1.0",
          "projectId": "<project UUID>",
          "eventType": "agent.message",
          "occurredAt": "2026-11-15T16:00:00Z",
          "data": {
            "threadId": "<thread UUID>",
            "sequenceNumber": 2,
            "type": "Message",
            "role": "Agent",
            "messageStatus": "Delivered",
            "content": "The requested work is complete."
          }
        }
        ```

        Deliveries are at-least-once. Deduplicate by `eventId`, preserve per-thread ordering with `threadId` plus `sequenceNumber`, and return a `2xx` response only after durably recording the event. `408`, `429`, connection failures, timeouts, and `5xx` responses are retried; other non-`2xx` responses are not automatically retried. Use the timeline endpoint only to reconcile a missed or out-of-order delivery.

        Start a thread with an initial user prompt. Use a `Queued` message to deliver a follow-up after the current run and a `Steering` message to request cooperative cancellation of an active run.

        All identifiers in the paths are UUIDs. Thread and timeline reads are scoped to the supplied project ID; a resource in a different project returns `404`.

        Authenticate every request with an agent API key using `Authorization: Bearer <key>`. Each key is scoped to one configured agent and cannot access another agent's threads, including threads in the same project.
        """;
    return Task.CompletedTask;
}));
builder.Services.AddHealthChecks();
builder.Services.AddAuthentication(AgentApiKeyAuthenticationDefaults.Scheme)
    .AddScheme<AuthenticationSchemeOptions, AgentApiKeyAuthenticationHandler>(AgentApiKeyAuthenticationDefaults.Scheme, _ => { });
builder.Services.AddAuthorization(options => options.AddPolicy("agent-api", policy =>
{
    policy.AddAuthenticationSchemes(AgentApiKeyAuthenticationDefaults.Scheme);
    policy.RequireAuthenticatedUser();
}));

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.MapHealthChecks("/health");
app.UseAuthentication();
app.UseAuthorization();
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
    .RequireAuthorization("agent-api")
    .WithTags("Agent threads")
    .WithDescription("Durable conversation lifecycle and timeline operations. A timeline entry is either a user/agent message or a tool call recorded during execution.");
agentThreads.MapGet("/", async (Guid projectId, ClaimsPrincipal user, AgentThreadService service, CancellationToken ct) =>
    IsAuthorizedProject(user, projectId) ? Results.Ok((await service.ListAsync(projectId, ct)).Where(thread => thread.AgentId == AgentId(user))) : Results.NotFound())
    .WithName("ListAgentThreads")
    .WithSummary("List a project's agent threads")
    .WithDescription("Returns every thread in the project, newest first. Filter the returned `agentId` client-side when an application is concerned with one agent.")
    .Produces<IReadOnlyList<AgentThreadSummary>>(StatusCodes.Status200OK);
agentThreads.MapGet("/{threadId:guid}", async (Guid projectId, Guid threadId, ClaimsPrincipal user, AgentThreadService service, CancellationToken ct) =>
{
    var thread = await service.FindAsync(projectId, threadId, ct);
    return thread is null || !IsAuthorized(user, projectId, thread.AgentId) ? Results.NotFound() : Results.Ok(thread);
})
    .WithName("GetAgentThread")
    .WithSummary("Get an agent thread")
    .WithDescription("Use this to check the current lifecycle status. A thread starts as `Running`; executions may later become `Completed`, `Failed`, or another terminal state.")
    .Produces<AgentThreadSummary>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status404NotFound);
agentThreads.MapGet("/{threadId:guid}/timeline", async (Guid projectId, Guid threadId, long? afterSequence, int? limit, ClaimsPrincipal user, AgentThreadService service, CancellationToken ct) =>
{
    try
    {
        var thread = await service.FindAsync(projectId, threadId, ct);
        if (thread is null || !IsAuthorized(user, projectId, thread.AgentId)) return Results.NotFound();
        var page = await service.ListEntriesAsync(projectId, threadId, afterSequence, limit ?? 50, ct);
        return page is null ? Results.NotFound() : Results.Ok(page);
    }
    catch (ArgumentOutOfRangeException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["pagination"] = [exception.Message] });
    }
})
    .WithName("ListAgentThreadTimeline")
    .WithSummary("Reconcile a thread's ordered timeline")
    .WithDescription("Webhooks are the primary notification mechanism. Use this endpoint only to reconcile a missed or out-of-order webhook delivery. Set `afterSequence` to the highest durably processed sequence number. `limit` defaults to 50 and must be from 1 through 100. A null `nextCursor` means the returned page reached the current end of the timeline.")
    .Produces<AgentThreadPage>(StatusCodes.Status200OK)
    .ProducesValidationProblem(StatusCodes.Status400BadRequest)
    .Produces(StatusCodes.Status404NotFound);
agentThreads.MapDelete("/{threadId:guid}", async (Guid projectId, Guid threadId, ClaimsPrincipal user, AgentThreadService service, CancellationToken ct) =>
{
    var thread = await service.FindAsync(projectId, threadId, ct);
    return thread is not null && IsAuthorized(user, projectId, thread.AgentId) && await service.DeleteAsync(projectId, threadId, ct) ? Results.NoContent() : Results.NotFound();
})
    .WithName("DeleteAgentThread")
    .WithSummary("Delete an agent thread")
    .WithDescription("Permanently deletes the thread and its execution history. This action cannot be undone.")
    .Produces(StatusCodes.Status204NoContent)
    .Produces(StatusCodes.Status404NotFound);
agentThreads.MapPost("/{threadId:guid}/messages", async (Guid projectId, Guid threadId, SubmitAgentMessageRequest request, ClaimsPrincipal user, AgentThreadService service, CancellationToken ct) =>
{
    try
    {
        var thread = await service.FindAsync(projectId, threadId, ct);
        if (thread is null || !IsAuthorized(user, projectId, thread.AgentId)) return Results.NotFound();
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
    .WithDescription("Submit a non-empty `content` value of at most 100,000 characters. Set `delivery` to `Queued` for the next execution turn; at most one queued message may be pending, otherwise the API returns `409`. Set it to `Steering` to add a message immediately and request cooperative cancellation of a running execution. `Initial` is reserved for thread creation and returns `400` here. A `202` means the message was stored; receive the later agent outcome through the configured `agent.message` webhook.\n\nExample request:\n```json\n{\n  \"content\": \"Use the existing project configuration and create the release plan.\",\n  \"delivery\": \"Queued\"\n}\n```")
    .Accepts<SubmitAgentMessageRequest>("application/json")
    .Produces<SubmittedThreadMessage>(StatusCodes.Status202Accepted)
    .ProducesValidationProblem(StatusCodes.Status400BadRequest)
    .Produces(StatusCodes.Status404NotFound)
    .Produces(StatusCodes.Status409Conflict);

app.MapPost("/api/projects/{projectId:guid}/agents/{agentId:guid}/threads", async (Guid projectId, Guid agentId, TriggerAgentThreadRequest request, ClaimsPrincipal user, AgentThreadService service, CancellationToken ct) =>
{
    try
    {
        if (!IsAuthorized(user, projectId, agentId)) return Results.NotFound();
        var thread = await service.TriggerAsync(projectId, agentId, request.InitialPrompt ?? string.Empty, ct);
        return thread is null ? Results.NotFound() : Results.Created($"/api/projects/{projectId}/agent-threads/{thread.Id}", thread);
    }
    catch (ArgumentException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["initialPrompt"] = [exception.Message] });
    }
})
    .RequireAuthorization("agent-api")
    .WithName("CreateAgentThread")
    .WithSummary("Create and start an agent thread")
    .WithDescription("Creates a durable thread for the specified agent, records `initialPrompt` as its first user message, and starts execution. Supply a non-empty prompt of at most 100,000 characters. A `201` confirms acceptance, not a completed agent response; receive messages and optional tool activity through the configured project webhook.\n\nExample request:\n```json\n{\n  \"initialPrompt\": \"Review the deployment request and tell me what information is missing.\"\n}\n```")
    .Accepts<TriggerAgentThreadRequest>("application/json")
    .Produces<AgentThreadSummary>(StatusCodes.Status201Created)
    .ProducesValidationProblem(StatusCodes.Status400BadRequest)
    .Produces(StatusCodes.Status404NotFound);

app.Run();

static Guid AgentId(ClaimsPrincipal user) => Guid.Parse(user.FindFirstValue(AgentApiKeyAuthenticationDefaults.AgentIdClaimType)!);
static bool IsAuthorizedProject(ClaimsPrincipal user, Guid projectId) => user.FindFirstValue(AgentApiKeyAuthenticationDefaults.ProjectIdClaimType) == projectId.ToString();
static bool IsAuthorized(ClaimsPrincipal user, Guid projectId, Guid agentId) => IsAuthorizedProject(user, projectId) && AgentId(user) == agentId;

public sealed record TriggerAgentThreadRequest(string? InitialPrompt);
public sealed record SubmitAgentMessageRequest(string? Content, AgentMessageDelivery Delivery);
