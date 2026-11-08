using System.Text.Json;
using AgentPlatform.Application.Projects;

namespace AgentPlatform.Api.Mcp;

public static class PlatformMcpEndpoint
{
    private const string ProtocolVersion = "2025-03-26";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static IEndpointRouteBuilder MapPlatformMcp(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/mcp", HandleAsync);
        return endpoints;
    }

    private static async Task<IResult> HandleAsync(HttpContext context, ProjectService projects, CancellationToken cancellationToken)
    {
        JsonDocument document;
        try
        {
            document = await JsonDocument.ParseAsync(context.Request.Body, cancellationToken: cancellationToken);
        }
        catch (JsonException)
        {
            return RpcError(null, -32700, "Parse error.");
        }

        using (document)
        {
            var request = document.RootElement;
            if (request.ValueKind != JsonValueKind.Object || !request.TryGetProperty("jsonrpc", out var jsonrpc) || jsonrpc.ValueKind != JsonValueKind.String || jsonrpc.GetString() != "2.0" || !request.TryGetProperty("method", out var methodElement) || methodElement.ValueKind != JsonValueKind.String)
                return RpcError(null, -32600, "Invalid Request.");

            var method = methodElement.GetString()!;
            var hasId = request.TryGetProperty("id", out var id);
            var responseId = hasId ? id.Clone() : (JsonElement?)null;
            if (!hasId && method.StartsWith("notifications/", StringComparison.Ordinal))
                return Results.Accepted();
            if (!hasId)
                return RpcError(null, -32600, "Invalid Request.");

            object? result;
            switch (method)
            {
                case "initialize":
                    result = new
                    {
                        protocolVersion = ProtocolVersion,
                        capabilities = new { tools = new { listChanged = false } },
                        serverInfo = new { name = "agent-platform", version = "1.0.0" },
                    };
                    break;
                case "ping":
                    result = new { };
                    break;
                case "tools/list":
                    result = new
                    {
                        tools = new[]
                        {
                            new
                            {
                                name = "list_projects",
                                description = "List the projects available in Agent Platform.",
                                inputSchema = new { type = "object", properties = new { }, additionalProperties = false },
                            },
                        },
                    };
                    break;
                case "tools/call":
                    if (!request.TryGetProperty("params", out var parameters) || parameters.ValueKind != JsonValueKind.Object ||
                        !parameters.TryGetProperty("name", out var toolName) || toolName.ValueKind != JsonValueKind.String)
                        return RpcError(responseId, -32602, "Tool name is required.");

                    if (!string.Equals(toolName.GetString(), "list_projects", StringComparison.Ordinal))
                        return RpcError(responseId, -32602, "Unknown tool.");

                    var projectList = await projects.ListAsync(cancellationToken);
                    result = new
                    {
                        content = new[]
                        {
                            new { type = "text", text = JsonSerializer.Serialize(projectList, JsonOptions) },
                        },
                        isError = false,
                    };
                    break;
                default:
                    return RpcError(responseId, -32601, "Method not found.");
            }

            context.Response.Headers["MCP-Protocol-Version"] = ProtocolVersion;
            return Results.Json(new { jsonrpc = "2.0", id = responseId, result });
        }
    }

    private static IResult RpcError(JsonElement? id, int code, string message) =>
        Results.Json(new { jsonrpc = "2.0", id, error = new { code, message } }, statusCode: StatusCodes.Status200OK);
}
