using System.Security.Claims;
using AgentPlatform.Application.Accounts;
using Microsoft.AspNetCore.Authorization;

namespace AgentPlatform.Api.Authorization;

public sealed class PlatformAdministratorRequirement : IAuthorizationRequirement;

public sealed class PlatformAdministratorAuthorizationHandler(AccountService accounts)
    : AuthorizationHandler<PlatformAdministratorRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PlatformAdministratorRequirement requirement)
    {
        var clerkUserId = context.User.FindFirstValue("sub") ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrWhiteSpace(clerkUserId) && await accounts.IsAdministratorAsync(clerkUserId, CancellationToken.None))
            context.Succeed(requirement);
    }
}
