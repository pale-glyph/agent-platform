using System;
using AgentPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

#nullable disable

namespace AgentPlatform.Infrastructure.Persistence.Migrations;

[DbContext(typeof(PlatformDbContext))]
partial class PlatformDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasAnnotation("ProductVersion", "10.0.0");
        modelBuilder.HasAnnotation("Relational:MaxIdentifierLength", 63);

        modelBuilder.Entity("AgentPlatform.Domain.Llm.LlmConfiguration", item =>
        {
            item.Property<Guid>("Id").HasColumnType("uuid").HasColumnName("id");
            item.Property<DateTimeOffset>("CreatedAt").HasColumnType("timestamp with time zone").HasColumnName("created_at");
            item.Property<string>("Name").IsRequired().HasMaxLength(200).HasColumnType("character varying(200)").HasColumnName("name");
            item.Property<string>("ProviderUrl").IsRequired().HasMaxLength(2000).HasColumnType("character varying(2000)").HasColumnName("provider_url");
            item.Property<string>("ModelName").IsRequired().HasMaxLength(500).HasColumnType("character varying(500)").HasColumnName("model_name");
            item.Property<string>("ApiKey").IsRequired().HasMaxLength(10000).HasColumnType("character varying(10000)").HasColumnName("api_key");
            item.HasKey("Id"); item.HasIndex("Name").IsUnique(); item.ToTable("llm_configurations");
        });

        modelBuilder.Entity("AgentPlatform.Domain.Agents.AgentDefinition", item =>
        {
            item.Property<Guid>("Id").HasColumnType("uuid").HasColumnName("id");
            item.Property<DateTimeOffset>("CreatedAt").HasColumnType("timestamp with time zone").HasColumnName("created_at");
            item.Property<string>("Description").IsRequired().HasMaxLength(2000).HasColumnType("character varying(2000)").HasColumnName("description");
            item.Property<Guid?>("LlmConfigurationId").HasColumnType("uuid").HasColumnName("llm_configuration_id");
            item.Property<string>("Name").IsRequired().HasMaxLength(200).HasColumnType("character varying(200)").HasColumnName("name");
            item.Property<Guid>("ProjectId").HasColumnType("uuid").HasColumnName("project_id");
            item.Property<string>("SystemPrompt").IsRequired().HasMaxLength(20000).HasColumnType("character varying(20000)").HasColumnName("system_prompt");
            item.HasKey("Id");
            item.HasIndex("LlmConfigurationId");
            item.HasIndex("ProjectId", "Name").IsUnique();
            item.ToTable("agents");
        });

        modelBuilder.Entity("AgentPlatform.Domain.Agents.AgentMcpToolGrant", item =>
        {
            item.Property<Guid>("AgentId").HasColumnType("uuid").HasColumnName("agent_id");
            item.Property<Guid>("McpServerId").HasColumnType("uuid").HasColumnName("mcp_server_id");
            item.Property<string>("ToolName").IsRequired().HasMaxLength(500).HasColumnType("character varying(500)").HasColumnName("tool_name");
            item.HasKey("AgentId", "McpServerId", "ToolName");
            item.HasIndex("McpServerId");
            item.ToTable("agent_mcp_tool_grants");
        });

        modelBuilder.Entity("AgentPlatform.Domain.Projects.Project", item =>
        {
            item.Property<Guid>("Id").HasColumnType("uuid").HasColumnName("id");
            item.Property<DateTimeOffset>("CreatedAt").HasColumnType("timestamp with time zone").HasColumnName("created_at");
            item.Property<string>("Name").IsRequired().HasMaxLength(200).HasColumnType("character varying(200)").HasColumnName("name");
            item.HasKey("Id");
            item.ToTable("projects");
        });

        modelBuilder.Entity("AgentPlatform.Domain.Mcp.McpServer", item =>
        {
            item.Property<Guid>("Id").HasColumnType("uuid").HasColumnName("id");
            item.Property<string>("ApiKey").HasMaxLength(10000).HasColumnType("character varying(10000)").HasColumnName("api_key");
            item.Property<int>("CacheTtlSeconds").HasColumnType("integer").HasColumnName("cache_ttl_seconds");
            item.Property<DateTimeOffset?>("CacheExpiresAt").HasColumnType("timestamp with time zone").HasColumnName("cache_expires_at");
            item.Property<string>("CachedToolDefinitions").HasColumnType("jsonb").HasColumnName("cached_tool_definitions");
            item.Property<DateTimeOffset>("CreatedAt").HasColumnType("timestamp with time zone").HasColumnName("created_at");
            item.Property<string>("Endpoint").IsRequired().HasMaxLength(2000).HasColumnType("character varying(2000)").HasColumnName("endpoint");
            item.Property<string>("Name").IsRequired().HasMaxLength(200).HasColumnType("character varying(200)").HasColumnName("name");
            item.Property<Guid>("ProjectId").HasColumnType("uuid").HasColumnName("project_id");
            item.Property<AgentPlatform.Domain.Mcp.McpServerSecurity>("Security").IsRequired().HasConversion<string>().HasMaxLength(20).HasColumnType("character varying(20)").HasColumnName("security");
            item.HasKey("Id");
            item.HasIndex("ProjectId", "Name").IsUnique();
            item.ToTable("mcp_servers");
        });

        modelBuilder.Entity("AgentPlatform.Domain.Webhooks.WebhookEndpoint", item =>
        {
            item.Property<Guid>("Id").HasColumnType("uuid").HasColumnName("id");
            item.Property<DateTimeOffset?>("CircuitOpenUntil").HasColumnType("timestamp with time zone").HasColumnName("circuit_open_until");
            item.Property<int>("ConsecutiveFailures").HasColumnType("integer").HasColumnName("consecutive_failures");
            item.Property<DateTimeOffset>("CreatedAt").HasColumnType("timestamp with time zone").HasColumnName("created_at");
            item.Property<string>("EndpointUrl").IsRequired().HasMaxLength(2000).HasColumnType("character varying(2000)").HasColumnName("endpoint_url");
            item.Property<string>("EventsJson").IsRequired().HasColumnType("jsonb").HasColumnName("events");
            item.Property<bool>("IsEnabled").HasColumnType("boolean").HasColumnName("is_enabled");
            item.Property<string>("Name").IsRequired().HasMaxLength(200).HasColumnType("character varying(200)").HasColumnName("name");
            item.Property<Guid>("ProjectId").HasColumnType("uuid").HasColumnName("project_id");
            item.Property<string>("Secret").IsRequired().HasMaxLength(1000).HasColumnType("character varying(1000)").HasColumnName("secret");
            item.HasKey("Id");
            item.HasIndex("ProjectId", "Name").IsUnique();
            item.ToTable("webhook_endpoints");
        });

        modelBuilder.Entity("AgentPlatform.Domain.Webhooks.WebhookDelivery", item =>
        {
            item.Property<Guid>("Id").HasColumnType("uuid").HasColumnName("id");
            item.Property<int>("AttemptCount").HasColumnType("integer").HasColumnName("attempt_count");
            item.Property<int>("RetryAttemptCount").HasColumnType("integer").HasColumnName("retry_attempt_count");
            item.Property<DateTimeOffset?>("ClaimedUntil").HasColumnType("timestamp with time zone").HasColumnName("claimed_until");
            item.Property<DateTimeOffset?>("CompletedAt").HasColumnType("timestamp with time zone").HasColumnName("completed_at");
            item.Property<DateTimeOffset>("CreatedAt").HasColumnType("timestamp with time zone").HasColumnName("created_at");
            item.Property<Guid>("EventId").HasColumnType("uuid").HasColumnName("event_id");
            item.Property<string>("EventType").IsRequired().HasMaxLength(200).HasColumnType("character varying(200)").HasColumnName("event_type");
            item.Property<string>("EndpointUrl").IsRequired().HasMaxLength(2000).HasColumnType("character varying(2000)").HasColumnName("endpoint_url");
            item.Property<string>("LastError").HasMaxLength(2000).HasColumnType("character varying(2000)").HasColumnName("last_error");
            item.Property<int?>("LastStatusCode").HasColumnType("integer").HasColumnName("last_status_code");
            item.Property<DateTimeOffset>("NextAttemptAt").HasColumnType("timestamp with time zone").HasColumnName("next_attempt_at");
            item.Property<string>("Payload").IsRequired().HasColumnType("jsonb").HasColumnName("payload");
            item.Property<Guid>("ProjectId").HasColumnType("uuid").HasColumnName("project_id");
            item.Property<string>("SigningSecret").IsRequired().HasMaxLength(1000).HasColumnType("character varying(1000)").HasColumnName("signing_secret");
            item.Property<AgentPlatform.Domain.Webhooks.WebhookDeliveryStatus>("Status").IsRequired().HasConversion<string>().HasMaxLength(20).HasColumnType("character varying(20)").HasColumnName("status");
            item.Property<string>("WebhookName").IsRequired().HasMaxLength(200).HasColumnType("character varying(200)").HasColumnName("webhook_name");
            item.Property<Guid>("WebhookId").HasColumnType("uuid").HasColumnName("webhook_id");
            item.HasKey("Id");
            item.HasIndex("ProjectId", "Status", "CreatedAt");
            item.HasIndex("Status", "NextAttemptAt", "ClaimedUntil");
            item.ToTable("webhook_deliveries");
        });

        modelBuilder.Entity("AgentPlatform.Domain.Agents.AgentDefinition", item =>
        {
            item.HasOne("AgentPlatform.Domain.Llm.LlmConfiguration", null).WithMany().HasForeignKey("LlmConfigurationId").OnDelete(DeleteBehavior.SetNull);
            item.HasOne("AgentPlatform.Domain.Projects.Project", null).WithMany().HasForeignKey("ProjectId").OnDelete(DeleteBehavior.Cascade).IsRequired();
        });

        modelBuilder.Entity("AgentPlatform.Domain.Agents.AgentMcpToolGrant", item =>
        {
            item.HasOne("AgentPlatform.Domain.Agents.AgentDefinition", null).WithMany("McpToolGrants").HasForeignKey("AgentId").OnDelete(DeleteBehavior.Cascade).IsRequired();
            item.HasOne("AgentPlatform.Domain.Mcp.McpServer", null).WithMany().HasForeignKey("McpServerId").OnDelete(DeleteBehavior.Cascade).IsRequired();
        });

        modelBuilder.Entity("AgentPlatform.Domain.Mcp.McpServer", item =>
        {
            item.HasOne("AgentPlatform.Domain.Projects.Project", null).WithMany().HasForeignKey("ProjectId").OnDelete(DeleteBehavior.Cascade).IsRequired();
        });

        modelBuilder.Entity("AgentPlatform.Domain.Webhooks.WebhookEndpoint", item =>
        {
            item.HasOne("AgentPlatform.Domain.Projects.Project", null).WithMany().HasForeignKey("ProjectId").OnDelete(DeleteBehavior.Cascade).IsRequired();
        });
    }
}
