using AgentPlatform.Application.Abstractions;
using AgentPlatform.Domain.Mcp;

namespace AgentPlatform.Application.Mcp;

public sealed record McpServerSummary(Guid Id, Guid ProjectId, string Name, string Endpoint, int CacheTtlSeconds, McpServerSecurity Security, DateTimeOffset? CacheExpiresAt, DateTimeOffset CreatedAt);
public sealed record McpToolDefinitions(string ToolsJson, bool FromCache);

public sealed class McpServerService(IProjectRepository projects, IMcpServerRepository servers, IMcpToolDefinitionClient toolDefinitions, TimeProvider timeProvider)
{
    public async Task<IReadOnlyList<McpServerSummary>?> ListAsync(Guid projectId, CancellationToken cancellationToken)
    {
        if (await projects.FindByIdAsync(projectId, cancellationToken) is null) return null;
        return (await servers.ListByProjectAsync(projectId, cancellationToken)).Select(ToSummary).ToArray();
    }

    public async Task<McpServerSummary?> CreateAsync(Guid projectId, string name, string endpoint, int cacheTtlSeconds, McpServerSecurity security, string? apiKey, CancellationToken cancellationToken)
    {
        if (await projects.FindByIdAsync(projectId, cancellationToken) is null) return null;
        var server = new McpServer(Guid.NewGuid(), projectId, name, endpoint, cacheTtlSeconds, security, apiKey, timeProvider.GetUtcNow());
        await servers.AddAsync(server, cancellationToken);
        await servers.SaveChangesAsync(cancellationToken);
        return ToSummary(server);
    }

    public async Task<McpServerSummary?> UpdateAsync(Guid projectId, Guid serverId, string name, string endpoint, int cacheTtlSeconds, McpServerSecurity security, string? apiKey, CancellationToken cancellationToken)
    {
        var server = await servers.FindByIdAsync(projectId, serverId, cancellationToken);
        if (server is null) return null;
        server.Update(name, endpoint, cacheTtlSeconds, security, apiKey);
        await servers.SaveChangesAsync(cancellationToken);
        return ToSummary(server);
    }

    public async Task<bool> DeleteAsync(Guid projectId, Guid serverId, CancellationToken cancellationToken)
    {
        var server = await servers.FindByIdAsync(projectId, serverId, cancellationToken);
        if (server is null) return false;
        servers.Remove(server);
        await servers.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> ClearCacheAsync(Guid projectId, Guid serverId, CancellationToken cancellationToken)
    {
        var server = await servers.FindByIdAsync(projectId, serverId, cancellationToken);
        if (server is null) return false;
        server.ClearToolDefinitionCache();
        await servers.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<McpToolDefinitions?> GetToolDefinitionsAsync(Guid projectId, Guid serverId, CancellationToken cancellationToken)
    {
        var server = await servers.FindByIdAsync(projectId, serverId, cancellationToken);
        if (server is null) return null;
        if (server.HasValidToolDefinitionCache(timeProvider.GetUtcNow())) return new(server.CachedToolDefinitions!, true);
        var definitions = await toolDefinitions.ListToolsAsync(server, cancellationToken);
        server.CacheToolDefinitions(definitions, timeProvider.GetUtcNow());
        await servers.SaveChangesAsync(cancellationToken);
        return new(definitions, false);
    }

    private static McpServerSummary ToSummary(McpServer server) => new(server.Id, server.ProjectId, server.Name, server.Endpoint, server.CacheTtlSeconds, server.Security, server.CacheExpiresAt, server.CreatedAt);
}
