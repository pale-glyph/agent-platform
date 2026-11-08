using System.Net.Http.Json;
using System.Text.Json;
using AgentPlatform.Application.Runtime;
using AgentPlatform.Domain.Mcp;

namespace AgentPlatform.Infrastructure.Runtime;

public sealed class HttpMcpToolClient(IHttpClientFactory httpClientFactory) : IMcpToolClient
{
    public async Task<string> CallAsync(McpServer server, string toolName, string argumentsJson, CancellationToken cancellationToken)
    {
        using var arguments = JsonDocument.Parse(argumentsJson);
        if (arguments.RootElement.ValueKind != JsonValueKind.Object) throw new InvalidOperationException("Tool arguments must be a JSON object.");
        using var request = new HttpRequestMessage(HttpMethod.Post, server.Endpoint)
        {
            Content = JsonContent.Create(new { jsonrpc = "2.0", id = Guid.NewGuid().ToString("N"), method = "tools/call", @params = new { name = toolName, arguments = arguments.RootElement } }),
        };
        request.Headers.Accept.ParseAdd("application/json, text/event-stream");
        if (server.Security == McpServerSecurity.ApiKey) request.Headers.Authorization = new("Bearer", server.ApiKey);
        using var response = await httpClientFactory.CreateClient("mcp").SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        if (!document.RootElement.TryGetProperty("result", out var result))
            throw new InvalidOperationException("The MCP server returned an invalid tools/call response.");
        return result.GetRawText();
    }
}
