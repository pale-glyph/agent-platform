using AgentPlatform.Application.Abstractions;
using AgentPlatform.Infrastructure.Persistence;
using AgentPlatform.Infrastructure.Mcp;
using AgentPlatform.Infrastructure.Runtime;
using AgentPlatform.Application.Runtime;
using AgentPlatform.Infrastructure.Webhooks;
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
        services.AddScoped<IAgentInstanceRepository, EfAgentInstanceRepository>();
        services.AddScoped<ILlmConfigurationRepository, EfLlmConfigurationRepository>();
        services.AddScoped<IMcpServerRepository, EfMcpServerRepository>();
        services.AddHttpClient("mcp");
        services.AddScoped<IMcpToolDefinitionClient, HttpMcpToolDefinitionClient>();
        services.AddHttpClient("openai-compatible", client => client.Timeout = TimeSpan.FromMinutes(2));
        services.AddScoped<IOpenAiCompatibleClient, OpenAiCompatibleClient>();
        services.AddScoped<IMcpToolClient, HttpMcpToolClient>();
        services.AddScoped<IWebhookRepository, EfWebhookRepository>();
        services.AddScoped<IWebhookEventPublisher, EfWebhookEventPublisher>();
        services.AddScoped<IWebhookDeliveryStore, EfWebhookDeliveryStore>();
        services.AddHttpClient("webhooks", client => client.Timeout = TimeSpan.FromSeconds(15));
        return services;
    }
}
