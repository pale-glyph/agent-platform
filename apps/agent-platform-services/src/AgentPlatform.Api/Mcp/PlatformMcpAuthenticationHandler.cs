using System.Security.Claims;
using System.Text.Encodings.Web;
using AgentPlatform.Application.Abstractions;
using AgentPlatform.Application.Agents;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace AgentPlatform.Api.Mcp;

public static class PlatformMcpAuthenticationDefaults
{
    public const string Scheme = "PlatformMcpAgentApiKey";
    public const string AgentIdClaimType = "agent_id";
    public const string ProjectIdClaimType = "project_id";
}

/// <summary>Authenticates platform MCP callers with a key bound to one agent and project.</summary>
public sealed class PlatformMcpAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IAgentApiKeyRepository keys)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authorization = Request.Headers.Authorization.ToString();
        const string bearerPrefix = "Bearer ";
        if (!authorization.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(authorization[bearerPrefix.Length..]))
            return AuthenticateResult.Fail("An agent API key must be supplied as a Bearer token.");

        var authentication = await keys.FindAuthenticationAsync(
            AgentApiKeyService.Hash(authorization[bearerPrefix.Length..].Trim()),
            Context.RequestAborted);
        if (authentication is null) return AuthenticateResult.Fail("The API key is invalid.");

        var identity = new ClaimsIdentity(
        [
            new Claim(PlatformMcpAuthenticationDefaults.AgentIdClaimType, authentication.AgentId.ToString()),
            new Claim(PlatformMcpAuthenticationDefaults.ProjectIdClaimType, authentication.ProjectId.ToString()),
        ], PlatformMcpAuthenticationDefaults.Scheme);
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), PlatformMcpAuthenticationDefaults.Scheme));
    }
}
