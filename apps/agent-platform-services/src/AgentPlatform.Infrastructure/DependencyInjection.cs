using Microsoft.Extensions.DependencyInjection;

namespace AgentPlatform.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // PostgreSQL, NATS JetStream, and outbox registrations will be added here.
        return services;
    }
}
