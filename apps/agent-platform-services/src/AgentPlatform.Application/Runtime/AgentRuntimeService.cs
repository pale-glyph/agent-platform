using AgentPlatform.Application.Abstractions;
using AgentPlatform.Application.Mcp;
using AgentPlatform.Domain.Instances;
using AgentPlatform.Domain.Webhooks;
using System.Text.Json;

namespace AgentPlatform.Application.Runtime;

/// <summary>Runs one persisted agent conversation until the model produces a final response.</summary>
public sealed class AgentRuntimeService(
    IAgentInstanceRepository instances,
    IAgentRepository agents,
    ILlmConfigurationRepository llmConfigurations,
    IMcpServerRepository mcpServers,
    McpServerService mcpServerService,
    IWebhookEventPublisher webhookEvents,
    IOpenAiCompatibleClient llm,
    IMcpToolClient mcpTools,
    TimeProvider timeProvider)
{
    private const int MaximumToolRounds = 8;

    public async Task<bool> RunAsync(Guid projectId, Guid instanceId, CancellationToken cancellationToken)
    {
        var instance = await instances.FindAsync(projectId, instanceId, cancellationToken);
        if (instance is null || instance.Status != AgentInstanceStatus.Running) return false;

        var agent = await agents.FindByIdAsync(projectId, instance.AgentId, cancellationToken)
            ?? throw new InvalidOperationException("The instance's agent no longer exists.");
        if (agent.LlmConfigurationId is null) throw new InvalidOperationException("The agent does not have an LLM configuration.");
        var configuration = await llmConfigurations.FindAsync(agent.LlmConfigurationId.Value, cancellationToken)
            ?? throw new InvalidOperationException("The agent's LLM configuration no longer exists.");

        var grantedTools = new Dictionary<string, (Guid ServerId, string ToolName)>(StringComparer.Ordinal);
        foreach (var grant in agent.McpToolGrants)
        {
            if (!grantedTools.TryAdd(grant.ToolName, (grant.McpServerId, grant.ToolName)))
                throw new InvalidOperationException($"Tool name '{grant.ToolName}' is granted by more than one MCP server. Tool names must be unique per agent.");
        }

        var toolDefinitions = await GetGrantedToolDefinitionsAsync(projectId, grantedTools, cancellationToken);
        var messages = (await instances.ListEntriesAsync(instance.Id, null, 10_000, cancellationToken))
            .Where(entry => entry.Type == AgentExecutionEntryType.Message && entry.MessageStatus != AgentMessageStatus.Cancelled)
            .Select(entry => new RuntimeChatMessage(entry.Role == AgentMessageRole.User ? "user" : "assistant", entry.Content))
            .Prepend(new RuntimeChatMessage("system", agent.SystemPrompt))
            .ToList();

        for (var round = 0; round < MaximumToolRounds; round++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var completion = await llm.CompleteAsync(configuration, messages, toolDefinitions, cancellationToken);
            if (completion.ToolCalls.Count == 0)
            {
                var content = completion.Content;
                if (string.IsNullOrWhiteSpace(content)) throw new InvalidOperationException("The model returned neither content nor tool calls.");
                var response = AgentExecutionEntry.Message(Guid.NewGuid(), instance.Id, instance.AllocateSequenceNumber(), AgentMessageRole.Agent, AgentMessageDelivery.Initial, AgentMessageStatus.Delivered, content, timeProvider.GetUtcNow());
                await instances.AddEntryAsync(response, cancellationToken);
                await webhookEvents.EnqueueAsync(projectId, WebhookEventTypes.AgentMessage, ToWebhookPayload(response), cancellationToken);
                instance.Complete();
                await instances.SaveChangesAsync(cancellationToken);
                return true;
            }

            messages.Add(new RuntimeChatMessage("assistant", completion.Content, ToolCalls: completion.ToolCalls));
            foreach (var call in completion.ToolCalls)
            {
                if (!grantedTools.TryGetValue(call.Name, out var grant))
                    throw new InvalidOperationException($"The model requested tool '{call.Name}', which is not granted to this agent.");
                var server = await mcpServers.FindByIdAsync(projectId, grant.ServerId, cancellationToken)
                    ?? throw new InvalidOperationException("A granted MCP server no longer exists.");
                var result = await mcpTools.CallAsync(server, grant.ToolName, call.ArgumentsJson, cancellationToken);
                var entry = AgentExecutionEntry.ToolCall(Guid.NewGuid(), instance.Id, instance.AllocateSequenceNumber(), call.Name, call.ArgumentsJson, result, timeProvider.GetUtcNow());
                await instances.AddEntryAsync(entry, cancellationToken);
                await webhookEvents.EnqueueAsync(projectId, WebhookEventTypes.AgentToolCall, ToWebhookPayload(entry), cancellationToken);
                await instances.SaveChangesAsync(cancellationToken);
                messages.Add(new RuntimeChatMessage("tool", result, call.Id));
            }
        }

        throw new InvalidOperationException($"The agent exceeded the {MaximumToolRounds} tool-call round limit.");
    }

    public async Task MarkFailedAsync(Guid projectId, Guid instanceId, CancellationToken cancellationToken)
    {
        var instance = await instances.FindAsync(projectId, instanceId, cancellationToken);
        if (instance is null || instance.Status != AgentInstanceStatus.Running) return;
        instance.Fail();
        await instances.SaveChangesAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<RuntimeToolDefinition>> GetGrantedToolDefinitionsAsync(
        Guid projectId,
        IReadOnlyDictionary<string, (Guid ServerId, string ToolName)> grants,
        CancellationToken cancellationToken)
    {
        var definitions = new List<RuntimeToolDefinition>();
        foreach (var group in grants.GroupBy(item => item.Value.ServerId))
        {
            var tools = await mcpServerService.GetToolDefinitionsAsync(projectId, group.Key, cancellationToken)
                ?? throw new InvalidOperationException("A granted MCP server no longer exists.");
            using var document = JsonDocument.Parse(tools.ToolsJson);
            var available = document.RootElement.EnumerateArray()
                .Where(tool => tool.TryGetProperty("name", out var name) && name.ValueKind == JsonValueKind.String)
                .ToDictionary(tool => tool.GetProperty("name").GetString()!, tool => tool, StringComparer.Ordinal);
            foreach (var grant in group)
            {
                if (!available.TryGetValue(grant.Value.ToolName, out var tool))
                    throw new InvalidOperationException($"Granted MCP tool '{grant.Value.ToolName}' is not advertised by its server.");
                var description = tool.TryGetProperty("description", out var descriptionValue) && descriptionValue.ValueKind == JsonValueKind.String
                    ? descriptionValue.GetString() ?? grant.Key
                    : grant.Key;
                var schema = tool.TryGetProperty("inputSchema", out var inputSchema) && inputSchema.ValueKind == JsonValueKind.Object
                    ? inputSchema.GetRawText()
                    : "{\"type\":\"object\",\"additionalProperties\":true}";
                definitions.Add(new RuntimeToolDefinition(grant.Key, description, schema));
            }
        }
        return definitions;
    }

    private static object ToWebhookPayload(AgentExecutionEntry entry) => new
    {
        entry.Id,
        entry.InstanceId,
        entry.SequenceNumber,
        entry.Type,
        entry.Role,
        entry.Delivery,
        entry.MessageStatus,
        entry.Content,
        entry.ToolName,
        entry.ToolArguments,
        entry.ToolResult,
        entry.CreatedAt,
    };
}
