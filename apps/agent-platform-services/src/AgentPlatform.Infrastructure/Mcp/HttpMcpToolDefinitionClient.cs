using System.Net.Http.Json;
using System.Text.Json;
using AgentPlatform.Application.Abstractions;
using AgentPlatform.Domain.Mcp;

namespace AgentPlatform.Infrastructure.Mcp;

public sealed class HttpMcpToolDefinitionClient(IHttpClientFactory httpClientFactory) : IMcpToolDefinitionClient
{
    public async Task<string> ListToolsAsync(McpServer server, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, server.Endpoint)
        {
            Content = JsonContent.Create(new { jsonrpc = "2.0", id = Guid.NewGuid().ToString("N"), method = "tools/list", @params = new { } }),
        };
        request.Headers.Accept.ParseAdd("application/json, text/event-stream");
        if (server.Security == McpServerSecurity.ApiKey) request.Headers.Authorization = new("Bearer", server.ApiKey);
        using var response = await httpClientFactory.CreateClient("mcp").SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        if (!document.RootElement.TryGetProperty("result", out var result) || !result.TryGetProperty("tools", out var tools) || tools.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException("The MCP server returned an invalid tools/list response.");
        return tools.GetRawText();
    }
}
