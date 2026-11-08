using AgentPlatform.Domain.Agents;
using AgentPlatform.Domain.Llm;
using AgentPlatform.Domain.Mcp;
using AgentPlatform.Domain.Projects;
using AgentPlatform.Domain.Webhooks;
using Microsoft.EntityFrameworkCore;

namespace AgentPlatform.Infrastructure.Persistence;

public sealed class PlatformDbContext(DbContextOptions<PlatformDbContext> options) : DbContext(options)
{
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<AgentDefinition> Agents => Set<AgentDefinition>();
    public DbSet<LlmConfiguration> LlmConfigurations => Set<LlmConfiguration>();
    public DbSet<McpServer> McpServers => Set<McpServer>();
    public DbSet<WebhookEndpoint> WebhookEndpoints => Set<WebhookEndpoint>();
    public DbSet<WebhookDelivery> WebhookDeliveries => Set<WebhookDelivery>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var project = modelBuilder.Entity<Project>();
        project.ToTable("projects");
        project.HasKey(item => item.Id);
        project.Property(item => item.Id).HasColumnName("id");
        project.Property(item => item.Name).HasColumnName("name").HasMaxLength(Project.MaximumNameLength).IsRequired();
        project.Property(item => item.CreatedAt).HasColumnName("created_at").IsRequired();

        var agent = modelBuilder.Entity<AgentDefinition>();
        agent.ToTable("agents");
        agent.HasKey(item => item.Id);
        agent.Property(item => item.Id).HasColumnName("id");
        agent.Property(item => item.ProjectId).HasColumnName("project_id");
        agent.Property(item => item.Name).HasColumnName("name").HasMaxLength(AgentDefinition.MaximumNameLength).IsRequired();
        agent.Property(item => item.Description).HasColumnName("description").HasMaxLength(AgentDefinition.MaximumDescriptionLength).IsRequired();
        agent.Property(item => item.SystemPrompt).HasColumnName("system_prompt").HasMaxLength(AgentDefinition.MaximumSystemPromptLength).IsRequired();
        agent.Property(item => item.CreatedAt).HasColumnName("created_at").IsRequired();
        agent.HasIndex(item => new { item.ProjectId, item.Name }).IsUnique();
        agent.HasOne<Project>().WithMany().HasForeignKey(item => item.ProjectId).OnDelete(DeleteBehavior.Cascade);

        var llm = modelBuilder.Entity<LlmConfiguration>();
        llm.ToTable("llm_configurations"); llm.HasKey(item => item.Id);
        llm.Property(item => item.Id).HasColumnName("id"); llm.Property(item => item.Name).HasColumnName("name").HasMaxLength(LlmConfiguration.MaximumNameLength).IsRequired();
        llm.Property(item => item.ProviderUrl).HasColumnName("provider_url").HasMaxLength(LlmConfiguration.MaximumProviderUrlLength).IsRequired();
        llm.Property(item => item.ModelName).HasColumnName("model_name").HasMaxLength(LlmConfiguration.MaximumModelNameLength).IsRequired();
        llm.Property(item => item.ApiKey).HasColumnName("api_key").HasMaxLength(LlmConfiguration.MaximumApiKeyLength).IsRequired(); llm.Property(item => item.CreatedAt).HasColumnName("created_at").IsRequired(); llm.HasIndex(item => item.Name).IsUnique();

        var mcp = modelBuilder.Entity<McpServer>();
        mcp.ToTable("mcp_servers");
        mcp.HasKey(item => item.Id);
        mcp.Property(item => item.Id).HasColumnName("id");
        mcp.Property(item => item.ProjectId).HasColumnName("project_id");
        mcp.Property(item => item.Name).HasColumnName("name").HasMaxLength(McpServer.MaximumNameLength).IsRequired();
        mcp.Property(item => item.Endpoint).HasColumnName("endpoint").HasMaxLength(McpServer.MaximumEndpointLength).IsRequired();
        mcp.Property(item => item.CacheTtlSeconds).HasColumnName("cache_ttl_seconds");
        mcp.Property(item => item.Security).HasColumnName("security").HasConversion<string>().HasMaxLength(20).IsRequired();
        mcp.Property(item => item.ApiKey).HasColumnName("api_key").HasMaxLength(McpServer.MaximumApiKeyLength);
        mcp.Property(item => item.CachedToolDefinitions).HasColumnName("cached_tool_definitions").HasColumnType("jsonb");
        mcp.Property(item => item.CacheExpiresAt).HasColumnName("cache_expires_at");
        mcp.Property(item => item.CreatedAt).HasColumnName("created_at").IsRequired();
        mcp.HasIndex(item => new { item.ProjectId, item.Name }).IsUnique();
        mcp.HasOne<Project>().WithMany().HasForeignKey(item => item.ProjectId).OnDelete(DeleteBehavior.Cascade);

        var webhook = modelBuilder.Entity<WebhookEndpoint>();
        webhook.ToTable("webhook_endpoints");
        webhook.HasKey(item => item.Id);
        webhook.Property(item => item.Id).HasColumnName("id");
        webhook.Property(item => item.ProjectId).HasColumnName("project_id");
        webhook.Property(item => item.Name).HasColumnName("name").HasMaxLength(WebhookEndpoint.MaximumNameLength).IsRequired();
        webhook.Property(item => item.EndpointUrl).HasColumnName("endpoint_url").HasMaxLength(WebhookEndpoint.MaximumUrlLength).IsRequired();
        webhook.Property(item => item.Secret).HasColumnName("secret").HasMaxLength(WebhookEndpoint.MaximumSecretLength).IsRequired();
        webhook.Property(item => item.EventsJson).HasColumnName("events").HasColumnType("jsonb").IsRequired();
        webhook.Ignore(item => item.Events);
        webhook.Property(item => item.IsEnabled).HasColumnName("is_enabled").IsRequired();
        webhook.Property(item => item.ConsecutiveFailures).HasColumnName("consecutive_failures").IsRequired();
        webhook.Property(item => item.CircuitOpenUntil).HasColumnName("circuit_open_until");
        webhook.Property(item => item.CreatedAt).HasColumnName("created_at").IsRequired();
        webhook.HasIndex(item => new { item.ProjectId, item.Name }).IsUnique();
        webhook.HasOne<Project>().WithMany().HasForeignKey(item => item.ProjectId).OnDelete(DeleteBehavior.Cascade);

        var delivery = modelBuilder.Entity<WebhookDelivery>();
        delivery.ToTable("webhook_deliveries");
        delivery.HasKey(item => item.Id);
        delivery.Property(item => item.Id).HasColumnName("id");
        delivery.Property(item => item.ProjectId).HasColumnName("project_id");
        delivery.Property(item => item.WebhookId).HasColumnName("webhook_id");
        delivery.Property(item => item.WebhookName).HasColumnName("webhook_name").HasMaxLength(WebhookEndpoint.MaximumNameLength).IsRequired();
        delivery.Property(item => item.EventId).HasColumnName("event_id");
        delivery.Property(item => item.EventType).HasColumnName("event_type").HasMaxLength(200).IsRequired();
        delivery.Property(item => item.EndpointUrl).HasColumnName("endpoint_url").HasMaxLength(WebhookEndpoint.MaximumUrlLength).IsRequired();
        delivery.Property(item => item.SigningSecret).HasColumnName("signing_secret").HasMaxLength(WebhookEndpoint.MaximumSecretLength).IsRequired();
        delivery.Property(item => item.Payload).HasColumnName("payload").HasColumnType("jsonb").IsRequired();
        delivery.Property(item => item.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20).IsRequired();
        delivery.Property(item => item.AttemptCount).HasColumnName("attempt_count").IsRequired();
        delivery.Property(item => item.RetryAttemptCount).HasColumnName("retry_attempt_count").IsRequired();
        delivery.Property(item => item.NextAttemptAt).HasColumnName("next_attempt_at").IsRequired();
        delivery.Property(item => item.ClaimedUntil).HasColumnName("claimed_until");
        delivery.Property(item => item.LastStatusCode).HasColumnName("last_status_code");
        delivery.Property(item => item.LastError).HasColumnName("last_error").HasMaxLength(2_000);
        delivery.Property(item => item.CreatedAt).HasColumnName("created_at").IsRequired();
        delivery.Property(item => item.CompletedAt).HasColumnName("completed_at");
        delivery.HasIndex(item => new { item.ProjectId, item.Status, item.CreatedAt });
        delivery.HasIndex(item => new { item.Status, item.NextAttemptAt, item.ClaimedUntil });
    }
}
