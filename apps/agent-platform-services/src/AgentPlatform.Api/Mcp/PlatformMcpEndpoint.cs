using System.Text.Json;
using System.Security.Claims;
using AgentPlatform.Application.Abstractions;
using AgentPlatform.Application.Agents;
using AgentPlatform.Application.Mcp;
using AgentPlatform.Application.Projects;
using AgentPlatform.Application.Webhooks;
using AgentPlatform.Domain.Mcp;

namespace AgentPlatform.Api.Mcp;

public static class PlatformMcpEndpoint
{
    private const string ProtocolVersion = "2025-03-26";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static IEndpointRouteBuilder MapPlatformMcp(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/mcp", () => Results.Ok(new { name = "agent-platform", transport = "Streamable HTTP JSON-RPC", message = "Send MCP JSON-RPC requests to this endpoint with POST. Start with the initialize method.", protocolVersion = ProtocolVersion })).RequireAuthorization("platform-mcp");
        endpoints.MapPost("/mcp", HandleAsync).RequireAuthorization("platform-mcp");
        return endpoints;
    }

    private static async Task<IResult> HandleAsync(HttpContext context, ProjectService projects, AgentService agents, McpServerService mcpServers, WebhookService webhooks, IAgentRepository agentRepository, CancellationToken ct)
    {
        JsonDocument document;
        try { document = await JsonDocument.ParseAsync(context.Request.Body, cancellationToken: ct); }
        catch (JsonException) { return RpcError(null, -32700, "Parse error."); }

        using (document)
        {
            var request = document.RootElement;
            if (request.ValueKind != JsonValueKind.Object || !request.TryGetProperty("jsonrpc", out var jsonrpc) || jsonrpc.ValueKind != JsonValueKind.String || jsonrpc.GetString() != "2.0" || !request.TryGetProperty("method", out var methodElement) || methodElement.ValueKind != JsonValueKind.String) return RpcError(null, -32600, "Invalid Request.");
            var method = methodElement.GetString()!;
            var hasId = request.TryGetProperty("id", out var id);
            var responseId = hasId ? id.Clone() : (JsonElement?)null;
            if (!hasId && method.StartsWith("notifications/", StringComparison.Ordinal)) return Results.Accepted();
            if (!hasId) return RpcError(null, -32600, "Invalid Request.");

            object? result = method switch
            {
                "initialize" => new { protocolVersion = ProtocolVersion, capabilities = new { tools = new { listChanged = false } }, serverInfo = new { name = "agent-platform", version = "1.0.0" } },
                "ping" => new { },
                "tools/list" => await HandleToolListAsync(context.User, agentRepository, ct),
                "tools/call" => await HandleToolCallAsync(request, context.User, projects, agents, mcpServers, webhooks, agentRepository, ct),
                _ => null,
            };
            if (result is null) return RpcError(responseId, -32601, "Method not found.");
            context.Response.Headers["MCP-Protocol-Version"] = ProtocolVersion;
            return Results.Json(new { jsonrpc = "2.0", id = responseId, result });
        }
    }

    private static async Task<object> HandleToolListAsync(ClaimsPrincipal user, IAgentRepository agentRepository, CancellationToken ct)
    {
        var agent = await CallingAgentAsync(user, agentRepository, ct);
        var grantedNames = agent.McpToolGrants.Select(grant => grant.ToolName).ToHashSet(StringComparer.Ordinal);
        return new { tools = ToolDefinitions.Where(tool => grantedNames.Contains(tool.Name)).ToArray() };
    }

    private static async Task<object> HandleToolCallAsync(JsonElement request, ClaimsPrincipal user, ProjectService projects, AgentService agents, McpServerService mcpServers, WebhookService webhooks, IAgentRepository agentRepository, CancellationToken ct)
    {
        if (!request.TryGetProperty("params", out var parameters) || parameters.ValueKind != JsonValueKind.Object || !parameters.TryGetProperty("name", out var toolName) || toolName.ValueKind != JsonValueKind.String)
            return ToolResponse(Error("Tool name is required."));
        var arguments = parameters.TryGetProperty("arguments", out var nestedArguments) && nestedArguments.ValueKind == JsonValueKind.Object
            ? nestedArguments
            : parameters;
        return ToolResponse(await ExecuteToolAsync(toolName.GetString()!, arguments, user, projects, agents, mcpServers, webhooks, agentRepository, ct));
    }

    private static object ToolResponse(ToolResult result) => new { content = new[] { new { type = "text", text = JsonSerializer.Serialize(result.Value, JsonOptions) } }, isError = result.IsError };

    private static async Task<ToolResult> ExecuteToolAsync(string toolName, JsonElement p, ClaimsPrincipal user, ProjectService projects, AgentService agents, McpServerService mcpServers, WebhookService webhooks, IAgentRepository agentRepository, CancellationToken ct)
    {
        try
        {
            var agent = await CallingAgentAsync(user, agentRepository, ct);
            if (!agent.McpToolGrants.Any(grant => string.Equals(grant.ToolName, toolName, StringComparison.Ordinal)))
                return Error("The calling agent is not granted this platform tool.");

            switch (toolName)
            {
                case "list_projects":
                {
                    var project = await projects.FindAsync(agent.ProjectId, ct);
                    return Success(project is null ? Array.Empty<object>() : new object[] { project });
                }
                case "get_project_details":
                {
                    var projectId = ScopedProjectId(agent, p);
                    var project = await projects.FindAsync(projectId, ct);
                    if (project is null) return NotFound("Project");
                    var agentList = await agents.ListAsync(projectId, ct);
                    var serverList = await mcpServers.ListAsync(projectId, ct);
                    var webhookList = await webhooks.ListAsync(projectId, ct);
                    return Success(new { project, agents = agentList, mcpServers = serverList, webhooks = webhookList });
                }
                case "create_project": return Error("Platform MCP agents are scoped to an existing project and cannot create projects.");
                case "update_project":
                {
                    var item = await projects.UpdateAsync(ScopedProjectId(agent, p), RequiredString(p, "name"), ct);
                    return item is null ? NotFound("Project") : Success(item);
                }
                case "delete_project": return await projects.DeleteAsync(ScopedProjectId(agent, p), ct) ? Success(new { deleted = true }) : NotFound("Project");
                case "create_agent":
                {
                    var item = await agents.CreateAsync(ScopedProjectId(agent, p), RequiredString(p, "name"), RequiredString(p, "description"), RequiredString(p, "systemPrompt"), OptionalGuid(p, "llmConfigurationId"), ReadToolGrants(p), [], ct);
                    return item is null ? NotFound("Project") : Success(item);
                }
                case "update_agent":
                {
                    var item = await agents.UpdateAsync(ScopedProjectId(agent, p), RequiredGuid(p, "agentId"), RequiredString(p, "name"), RequiredString(p, "description"), RequiredString(p, "systemPrompt"), OptionalGuid(p, "llmConfigurationId"), ReadToolGrants(p), [], ct);
                    return item is null ? NotFound("Agent") : Success(item);
                }
                case "delete_agent": return await agents.DeleteAsync(ScopedProjectId(agent, p), RequiredGuid(p, "agentId"), ct) ? Success(new { deleted = true }) : NotFound("Agent");
                case "create_mcp_server":
                {
                    var item = await mcpServers.CreateAsync(ScopedProjectId(agent, p), RequiredString(p, "name"), RequiredString(p, "endpoint"), RequiredInt(p, "cacheTtlSeconds"), RequiredSecurity(p), OptionalString(p, "apiKey"), ct);
                    return item is null ? NotFound("Project") : Success(item);
                }
                case "update_mcp_server":
                {
                    var item = await mcpServers.UpdateAsync(ScopedProjectId(agent, p), RequiredGuid(p, "serverId"), RequiredString(p, "name"), RequiredString(p, "endpoint"), RequiredInt(p, "cacheTtlSeconds"), RequiredSecurity(p), OptionalString(p, "apiKey"), ct);
                    return item is null ? NotFound("MCP server") : Success(item);
                }
                case "delete_mcp_server": return await mcpServers.DeleteAsync(ScopedProjectId(agent, p), RequiredGuid(p, "serverId"), ct) ? Success(new { deleted = true }) : NotFound("MCP server");
                case "create_webhook":
                {
                    var item = await webhooks.CreateAsync(ScopedProjectId(agent, p), RequiredString(p, "name"), RequiredString(p, "endpointUrl"), RequiredString(p, "secret"), RequiredStringArray(p, "events"), ct);
                    return item is null ? NotFound("Project") : Success(item);
                }
                case "update_webhook":
                {
                    var item = await webhooks.UpdateAsync(ScopedProjectId(agent, p), RequiredGuid(p, "webhookId"), RequiredString(p, "name"), RequiredString(p, "endpointUrl"), OptionalString(p, "secret"), RequiredStringArray(p, "events"), RequiredBool(p, "enabled"), ct);
                    return item is null ? NotFound("Webhook") : Success(item);
                }
                case "delete_webhook": return await webhooks.DeleteAsync(ScopedProjectId(agent, p), RequiredGuid(p, "webhookId"), ct) ? Success(new { deleted = true }) : NotFound("Webhook");
                default: return Error("Unknown tool.");
            }
        }
        catch (ArgumentException exception) { return Error(exception.Message); }
        catch (UnauthorizedAccessException) { return Error("The requested project is outside the calling agent's scope."); }
    }

    private static async Task<AgentPlatform.Domain.Agents.AgentDefinition> CallingAgentAsync(ClaimsPrincipal user, IAgentRepository agentRepository, CancellationToken ct)
    {
        if (!Guid.TryParse(user.FindFirstValue(PlatformMcpAuthenticationDefaults.AgentIdClaimType), out var agentId) ||
            !Guid.TryParse(user.FindFirstValue(PlatformMcpAuthenticationDefaults.ProjectIdClaimType), out var projectId))
            throw new UnauthorizedAccessException();
        return await agentRepository.FindByIdAsync(projectId, agentId, ct) ?? throw new UnauthorizedAccessException();
    }

    private static Guid ScopedProjectId(AgentPlatform.Domain.Agents.AgentDefinition agent, JsonElement parameters)
    {
        var projectId = RequiredGuid(parameters, "projectId");
        if (projectId != agent.ProjectId) throw new UnauthorizedAccessException();
        return projectId;
    }

    private static readonly ToolDefinition[] ToolDefinitions =
    [
        Tool("list_projects", "List all projects.", new { }),
        Tool("get_project_details", "Get a project and its agents, MCP servers, and webhooks.", new { projectId = "UUID" }, "projectId"),
        Tool("create_project", "Create a project.", new { name = "string" }, "name"),
        Tool("update_project", "Rename a project.", new { projectId = "UUID", name = "string" }, "projectId", "name"),
        Tool("delete_project", "Delete a project and its resources.", new { projectId = "UUID" }, "projectId"),
        Tool("create_agent", "Create an agent. Omit llmConfigurationId to create a misconfigured agent; MCP tool grants are deny-by-default.", new { projectId = "UUID", name = "string", description = "string", systemPrompt = "string", llmConfigurationId = "UUID or null", mcpToolGrants = "array of { mcpServerId, toolName }" }, "projectId", "name", "description", "systemPrompt"),
        Tool("update_agent", "Replace an agent's configuration and MCP tool grants.", new { projectId = "UUID", agentId = "UUID", name = "string", description = "string", systemPrompt = "string", llmConfigurationId = "UUID or null", mcpToolGrants = "array of { mcpServerId, toolName }" }, "projectId", "agentId", "name", "description", "systemPrompt"),
        Tool("delete_agent", "Delete an agent.", new { projectId = "UUID", agentId = "UUID" }, "projectId", "agentId"),
        Tool("create_mcp_server", "Create a project MCP server.", new { projectId = "UUID", name = "string", endpoint = "HTTP(S) URL", cacheTtlSeconds = "integer", security = "None or ApiKey", apiKey = "string when security is ApiKey" }, "projectId", "name", "endpoint", "cacheTtlSeconds", "security"),
        Tool("update_mcp_server", "Update a project MCP server. Omit apiKey to retain its existing key.", new { projectId = "UUID", serverId = "UUID", name = "string", endpoint = "HTTP(S) URL", cacheTtlSeconds = "integer", security = "None or ApiKey", apiKey = "string" }, "projectId", "serverId", "name", "endpoint", "cacheTtlSeconds", "security"),
        Tool("delete_mcp_server", "Delete a project MCP server and its associated tool grants.", new { projectId = "UUID", serverId = "UUID" }, "projectId", "serverId"),
        Tool("create_webhook", "Create a project webhook with an HTTP or HTTPS endpoint.", new { projectId = "UUID", name = "string", endpointUrl = "HTTP(S) URL", secret = "string", events = "array of event names" }, "projectId", "name", "endpointUrl", "secret", "events"),
        Tool("update_webhook", "Update a project webhook. Omit secret to retain its existing signing secret. The endpoint may use HTTP or HTTPS.", new { projectId = "UUID", webhookId = "UUID", name = "string", endpointUrl = "HTTP(S) URL", secret = "string", events = "array of event names", enabled = "boolean" }, "projectId", "webhookId", "name", "endpointUrl", "events", "enabled"),
        Tool("delete_webhook", "Delete a project webhook.", new { projectId = "UUID", webhookId = "UUID" }, "projectId", "webhookId"),
    ];

    private static ToolDefinition Tool(string name, string description, object properties, params string[] required)
    {
        var schemas = properties.GetType().GetProperties().ToDictionary(property => property.Name, property => PropertySchema((string)property.GetValue(properties)!));
        return new ToolDefinition(name, description, new Dictionary<string, object?> { ["type"] = "object", ["properties"] = schemas, ["required"] = required, ["additionalProperties"] = false });
    }

    private static object PropertySchema(string description)
    {
        if (description.StartsWith("integer", StringComparison.Ordinal)) return new { type = "integer" };
        if (description.StartsWith("boolean", StringComparison.Ordinal)) return new { type = "boolean" };
        if (description.StartsWith("array", StringComparison.Ordinal)) return new { type = "array" };
        if (description.StartsWith("UUID", StringComparison.Ordinal)) return description.Contains("null", StringComparison.Ordinal) ? new Dictionary<string, object?> { ["type"] = new[] { "string", "null" }, ["format"] = "uuid" } : new { type = "string", format = "uuid" };
        return new { type = "string" };
    }
    private static Guid RequiredGuid(JsonElement p, string name) => Guid.TryParse(RequiredString(p, name), out var id) ? id : throw new ArgumentException($"{name} must be a UUID.");
    private static Guid? OptionalGuid(JsonElement p, string name) { if (!p.TryGetProperty(name, out var value) || value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined) return null; return value.ValueKind == JsonValueKind.String && Guid.TryParse(value.GetString(), out var id) ? id : throw new ArgumentException($"{name} must be a UUID or null."); }
    private static string RequiredString(JsonElement p, string name) => OptionalString(p, name) ?? throw new ArgumentException($"{name} is required.");
    private static string? OptionalString(JsonElement p, string name) => p.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    private static int RequiredInt(JsonElement p, string name) => p.TryGetProperty(name, out var value) && value.TryGetInt32(out var result) ? result : throw new ArgumentException($"{name} must be an integer.");
    private static bool RequiredBool(JsonElement p, string name) => p.TryGetProperty(name, out var value) && (value.ValueKind is JsonValueKind.True or JsonValueKind.False) ? value.GetBoolean() : throw new ArgumentException($"{name} must be a boolean.");
    private static McpServerSecurity RequiredSecurity(JsonElement p) => Enum.TryParse<McpServerSecurity>(RequiredString(p, "security"), true, out var security) && Enum.IsDefined(security) ? security : throw new ArgumentException("security must be None or ApiKey.");
    private static IReadOnlyList<string> RequiredStringArray(JsonElement p, string name) { if (!p.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.Array) throw new ArgumentException($"{name} must be an array of strings."); return value.EnumerateArray().Select(item => item.ValueKind == JsonValueKind.String ? item.GetString() : throw new ArgumentException($"{name} must be an array of strings.")).OfType<string>().ToArray(); }
    private static IReadOnlyList<McpToolGrantSummary> ReadToolGrants(JsonElement p) { if (!p.TryGetProperty("mcpToolGrants", out var value) || value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined) return []; if (value.ValueKind != JsonValueKind.Array) throw new ArgumentException("mcpToolGrants must be an array."); return value.EnumerateArray().Select(item => item.ValueKind == JsonValueKind.Object ? new McpToolGrantSummary(RequiredGuid(item, "mcpServerId"), RequiredString(item, "toolName")) : throw new ArgumentException("Each MCP tool grant must be an object.")).ToArray(); }
    private static ToolResult Success(object? value) => new(value, false);
    private static ToolResult Error(string message) => new(new { error = message }, true);
    private static ToolResult NotFound(string resource) => Error($"{resource} was not found.");
    private static IResult RpcError(JsonElement? id, int code, string message) => Results.Json(new { jsonrpc = "2.0", id, error = new { code, message } }, statusCode: StatusCodes.Status200OK);
    private sealed record ToolDefinition(string Name, string Description, object InputSchema);
    private sealed record ToolResult(object? Value, bool IsError);
}
