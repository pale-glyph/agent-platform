using AgentPlatform.Domain.Agents;
using AgentPlatform.Domain.Llm;
using AgentPlatform.Domain.Mcp;
using AgentPlatform.Domain.Projects;
using AgentPlatform.Domain.Webhooks;
using AgentPlatform.Domain.Instances;
using Microsoft.EntityFrameworkCore;

namespace AgentPlatform.Infrastructure.Persistence;

public sealed class PlatformDbContext(DbContextOptions<PlatformDbContext> options) : DbContext(options)
{
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<AgentDefinition> Agents => Set<AgentDefinition>();
    public DbSet<AgentMcpToolGrant> AgentMcpToolGrants => Set<AgentMcpToolGrant>();
    public DbSet<LlmConfiguration> LlmConfigurations => Set<LlmConfiguration>();
    public DbSet<McpServer> McpServers => Set<McpServer>();
    public DbSet<WebhookEndpoint> WebhookEndpoints => Set<WebhookEndpoint>();
    public DbSet<WebhookDelivery> WebhookDeliveries => Set<WebhookDelivery>();
    public DbSet<AgentInstance> AgentInstances => Set<AgentInstance>();
    public DbSet<AgentExecutionEntry> AgentExecutionEntries => Set<AgentExecutionEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var instance = modelBuilder.Entity<AgentInstance>();
        instance.ToTable("agent_instances");
        instance.HasKey(item => item.Id);
        instance.Property(item => item.Id).HasColumnName("id");
        instance.Property(item => item.ProjectId).HasColumnName("project_id");
        instance.Property(item => item.AgentId).HasColumnName("agent_id");
        instance.Property(item => item.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20).IsRequired();
        instance.Property(item => item.NextSequenceNumber).HasColumnName("next_sequence_number").IsRequired();
        instance.Property(item => item.PendingQueuedMessageId).HasColumnName("pending_queued_message_id");
        instance.Property(item => item.CancellationRequestedAt).HasColumnName("cancellation_requested_at");
        instance.Property(item => item.CreatedAt).HasColumnName("created_at").IsRequired();
        instance.HasIndex(item => new { item.ProjectId, item.CreatedAt });
        instance.HasOne<AgentDefinition>().WithMany().HasForeignKey(item => item.AgentId).OnDelete(DeleteBehavior.Cascade);
        instance.HasOne<Project>().WithMany().HasForeignKey(item => item.ProjectId).OnDelete(DeleteBehavior.Cascade);

        var entry = modelBuilder.Entity<AgentExecutionEntry>();
        entry.ToTable("agent_execution_entries");
        entry.HasKey(item => item.Id);
        entry.Property(item => item.Id).HasColumnName("id");
        entry.Property(item => item.InstanceId).HasColumnName("instance_id");
        entry.Property(item => item.SequenceNumber).HasColumnName("sequence_number");
        entry.Property(item => item.Type).HasColumnName("type").HasConversion<string>().HasMaxLength(20).IsRequired();
        entry.Property(item => item.Role).HasColumnName("role").HasConversion<string>().HasMaxLength(20);
        entry.Property(item => item.Delivery).HasColumnName("delivery").HasConversion<string>().HasMaxLength(20);
        entry.Property(item => item.MessageStatus).HasColumnName("message_status").HasConversion<string>().HasMaxLength(20);
        entry.Property(item => item.Content).HasColumnName("content").HasMaxLength(AgentExecutionEntry.MaximumContentLength);
        entry.Property(item => item.ToolName).HasColumnName("tool_name").HasMaxLength(AgentExecutionEntry.MaximumToolNameLength);
        entry.Property(item => item.ToolArguments).HasColumnName("tool_arguments").HasMaxLength(AgentExecutionEntry.MaximumContentLength);
        entry.Property(item => item.ToolResult).HasColumnName("tool_result").HasMaxLength(AgentExecutionEntry.MaximumContentLength);
        entry.Property(item => item.CreatedAt).HasColumnName("created_at").IsRequired();
        entry.HasIndex(item => new { item.InstanceId, item.SequenceNumber }).IsUnique();
        entry.HasOne<AgentInstance>().WithMany().HasForeignKey(item => item.InstanceId).OnDelete(DeleteBehavior.Cascade);

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
        agent.Property(item => item.LlmConfigurationId).HasColumnName("llm_configuration_id");
        agent.Property(item => item.CreatedAt).HasColumnName("created_at").IsRequired();
        agent.HasIndex(item => new { item.ProjectId, item.Name }).IsUnique();
        agent.HasOne<Project>().WithMany().HasForeignKey(item => item.ProjectId).OnDelete(DeleteBehavior.Cascade);
        agent.HasOne<LlmConfiguration>().WithMany().HasForeignKey(item => item.LlmConfigurationId).OnDelete(DeleteBehavior.SetNull);

        var grant = modelBuilder.Entity<AgentMcpToolGrant>();
        grant.ToTable("agent_mcp_tool_grants");
        grant.HasKey(item => new { item.AgentId, item.McpServerId, item.ToolName });
        grant.Property(item => item.AgentId).HasColumnName("agent_id");
        grant.Property(item => item.McpServerId).HasColumnName("mcp_server_id");
        grant.Property(item => item.ToolName).HasColumnName("tool_name").HasMaxLength(AgentMcpToolGrant.MaximumToolNameLength);
        grant.HasOne<AgentDefinition>().WithMany(item => item.McpToolGrants).HasForeignKey(item => item.AgentId).OnDelete(DeleteBehavior.Cascade);
        grant.HasOne<McpServer>().WithMany().HasForeignKey(item => item.McpServerId).OnDelete(DeleteBehavior.Cascade);

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
