using AgentPlatform.Application.Abstractions;
using AgentPlatform.Application.Agents;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

public static class AgentApiKeyAuthenticationDefaults
{
    public const string Scheme = "AgentApiKey";
    public const string AgentIdClaimType = "agent_id";
    public const string ProjectIdClaimType = "project_id";
}

public sealed class AgentApiKeyAuthenticationHandler(
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
            return AuthenticateResult.Fail("An API key must be supplied as a Bearer token.");

        var authentication = await keys.FindAuthenticationAsync(AgentApiKeyService.Hash(authorization[bearerPrefix.Length..].Trim()), Context.RequestAborted);
        if (authentication is null) return AuthenticateResult.Fail("The API key is invalid.");

        var identity = new ClaimsIdentity(
        [
            new Claim(AgentApiKeyAuthenticationDefaults.AgentIdClaimType, authentication.AgentId.ToString()),
            new Claim(AgentApiKeyAuthenticationDefaults.ProjectIdClaimType, authentication.ProjectId.ToString()),
        ], AgentApiKeyAuthenticationDefaults.Scheme);
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), AgentApiKeyAuthenticationDefaults.Scheme));
    }
}
