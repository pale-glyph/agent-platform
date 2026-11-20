using AgentPlatform.Application.Abstractions;
using AgentPlatform.Infrastructure.Persistence;
using AgentPlatform.Infrastructure.Mcp;
using AgentPlatform.Infrastructure.Runtime;
using AgentPlatform.Application.Runtime;
using AgentPlatform.Infrastructure.Webhooks;
using AgentPlatform.Infrastructure.Networking;
using AgentPlatform.Infrastructure.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace AgentPlatform.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        var dataSourceConnectionString = new NpgsqlConnectionStringBuilder(connectionString)
        {
            GssEncryptionMode = GssEncryptionMode.Disable,
        }.ConnectionString;

        services.AddDbContext<PlatformDbContext>(options => options.UseNpgsql(dataSourceConnectionString));
        services.AddSingleton<IOutboundUrlPolicy, OutboundUrlPolicy>();
        services.AddSingleton<IAdministratorBootstrap, ConfigurationAdministratorBootstrap>();
        services.AddScoped<IAccountRepository, EfAccountRepository>();
        services.AddScoped<IProjectRepository, EfProjectRepository>();
        services.AddScoped<IAgentRepository, EfAgentRepository>();
        services.AddScoped<IAgentApiKeyRepository, EfAgentApiKeyRepository>();
        services.AddScoped<IAgentThreadRepository, EfAgentThreadRepository>();
        services.AddScoped<ILlmConfigurationRepository, EfLlmConfigurationRepository>();
        services.AddScoped<IMcpServerRepository, EfMcpServerRepository>();
        services.AddHttpClient("mcp", client => client.Timeout = TimeSpan.FromSeconds(30)).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
        services.AddScoped<IMcpToolDefinitionClient, HttpMcpToolDefinitionClient>();
        services.AddHttpClient("openai-compatible", client => client.Timeout = TimeSpan.FromMinutes(2)).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
        services.AddScoped<IOpenAiCompatibleClient, OpenAiCompatibleClient>();
        services.AddScoped<IMcpToolClient, HttpMcpToolClient>();
        services.AddScoped<IWebhookRepository, EfWebhookRepository>();
        services.AddScoped<IWebhookEventPublisher, EfWebhookEventPublisher>();
        services.AddScoped<IWebhookDeliveryStore, EfWebhookDeliveryStore>();
        services.AddScoped<IUsageMetricRepository, EfUsageMetricRepository>();
        services.AddScoped<IKnowledgeBaseGrantValidator, EfKnowledgeBaseGrantValidator>();
        services.AddHttpClient("webhooks", client => client.Timeout = TimeSpan.FromSeconds(15)).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
        return services;
    }
}
