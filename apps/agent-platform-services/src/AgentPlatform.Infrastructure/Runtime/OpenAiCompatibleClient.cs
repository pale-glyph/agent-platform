using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AgentPlatform.Application.Runtime;
using AgentPlatform.Domain.Llm;

namespace AgentPlatform.Infrastructure.Runtime;

/// <summary>Minimal Chat Completions adapter for OpenAI-compatible providers.</summary>
public sealed class OpenAiCompatibleClient(IHttpClientFactory httpClientFactory) : IOpenAiCompatibleClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<RuntimeCompletion> CompleteAsync(LlmConfiguration configuration, IReadOnlyList<RuntimeChatMessage> messages, IReadOnlyList<RuntimeToolDefinition> tools, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, CompletionEndpoint(configuration.ProviderUrl));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", configuration.ApiKey);
        var payload = new Dictionary<string, object?>
        {
            ["model"] = configuration.ModelName,
            ["messages"] = messages.Select(ToWireMessage),
        };
        if (tools.Count > 0)
            payload["tools"] = tools.Select(tool => new { type = "function", function = new { name = tool.Name, description = tool.Description, parameters = JsonSerializer.Deserialize<JsonElement>(tool.ParametersJson, JsonOptions) } });
        request.Content = JsonContent.Create(payload, options: JsonOptions);

        using var response = await httpClientFactory.CreateClient("openai-compatible").SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var body = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(body, cancellationToken: cancellationToken);
        var message = document.RootElement.GetProperty("choices")[0].GetProperty("message");
        var content = message.TryGetProperty("content", out var contentValue) && contentValue.ValueKind != JsonValueKind.Null ? contentValue.GetString() : null;
        var calls = message.TryGetProperty("tool_calls", out var toolCalls) && toolCalls.ValueKind == JsonValueKind.Array
            ? toolCalls.EnumerateArray().Select(ParseToolCall).ToArray()
            : [];
        return new RuntimeCompletion(content, calls);
    }

    private static IReadOnlyDictionary<string, object?> ToWireMessage(RuntimeChatMessage message)
    {
        var result = new Dictionary<string, object?> { ["role"] = message.Role, ["content"] = message.Content };
        if (message.ToolCallId is not null) result["tool_call_id"] = message.ToolCallId;
        if (message.ToolCalls is not null) result["tool_calls"] = message.ToolCalls.Select(call => new { id = call.Id, type = "function", function = new { name = call.Name, arguments = call.ArgumentsJson } });
        return result;
    }

    private static RuntimeToolCall ParseToolCall(JsonElement value)
    {
        var function = value.GetProperty("function");
        return new RuntimeToolCall(value.GetProperty("id").GetString() ?? throw new InvalidOperationException("The model returned a tool call without an id."), function.GetProperty("name").GetString() ?? throw new InvalidOperationException("The model returned a tool call without a name."), function.GetProperty("arguments").GetString() ?? "{}");
    }

    private static Uri CompletionEndpoint(string providerUrl)
    {
        var uri = new Uri(providerUrl, UriKind.Absolute);
        return uri.AbsolutePath.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase)
            ? uri
            : new Uri(uri.AbsoluteUri.TrimEnd('/') + "/v1/chat/completions", UriKind.Absolute);
    }
}
