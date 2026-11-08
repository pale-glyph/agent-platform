using AgentPlatform.Application.Abstractions;
using AgentPlatform.Infrastructure.Persistence;
using AgentPlatform.Infrastructure.Mcp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AgentPlatform.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<PlatformDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IProjectRepository, EfProjectRepository>();
        services.AddScoped<IAgentRepository, EfAgentRepository>();
        services.AddScoped<ILlmConfigurationRepository, EfLlmConfigurationRepository>();
        services.AddScoped<IMcpServerRepository, EfMcpServerRepository>();
        services.AddHttpClient("mcp");
        services.AddScoped<IMcpToolDefinitionClient, HttpMcpToolDefinitionClient>();
        return services;
    }
}
