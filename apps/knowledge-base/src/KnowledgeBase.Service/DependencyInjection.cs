using KnowledgeBase.Service.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace KnowledgeBase.Service;

public static class DependencyInjection
{
    public static IServiceCollection AddKnowledgeBasePersistence(this IServiceCollection services, string connectionString)
    {
        var configured = new NpgsqlConnectionStringBuilder(connectionString) { GssEncryptionMode = GssEncryptionMode.Disable }.ConnectionString;
        services.AddDbContext<KnowledgeBaseDbContext>(options => options.UseNpgsql(configured));
        return services;
    }
}
