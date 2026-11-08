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
            item.Property<string>("Name").IsRequired().HasMaxLength(200).HasColumnType("character varying(200)").HasColumnName("name");
            item.Property<Guid>("ProjectId").HasColumnType("uuid").HasColumnName("project_id");
            item.Property<string>("SystemPrompt").IsRequired().HasMaxLength(20000).HasColumnType("character varying(20000)").HasColumnName("system_prompt");
            item.HasKey("Id");
            item.HasIndex("ProjectId", "Name").IsUnique();
            item.ToTable("agents");
        });

        modelBuilder.Entity("AgentPlatform.Domain.Projects.Project", item =>
        {
            item.Property<Guid>("Id").HasColumnType("uuid").HasColumnName("id");
            item.Property<DateTimeOffset>("CreatedAt").HasColumnType("timestamp with time zone").HasColumnName("created_at");
            item.Property<string>("Name").IsRequired().HasMaxLength(200).HasColumnType("character varying(200)").HasColumnName("name");
            item.HasKey("Id");
            item.ToTable("projects");
        });

        modelBuilder.Entity("AgentPlatform.Domain.Agents.AgentDefinition", item =>
        {
            item.HasOne("AgentPlatform.Domain.Projects.Project", null).WithMany().HasForeignKey("ProjectId").OnDelete(DeleteBehavior.Cascade).IsRequired();
        });
    }
}
