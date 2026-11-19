using AgentPlatform.Application.Abstractions;
using Microsoft.Extensions.Configuration;

namespace AgentPlatform.Infrastructure.Authentication;

public sealed class ConfigurationAdministratorBootstrap(IConfiguration configuration) : IAdministratorBootstrap
{
    public bool IsBootstrapAdministrator(string clerkUserId) =>
        (configuration["Administration:BootstrapAdminClerkUserIds"] ?? string.Empty)
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Contains(clerkUserId, StringComparer.Ordinal);
}
