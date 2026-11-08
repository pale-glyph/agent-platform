using AgentPlatform.Domain.Agents;
using AgentPlatform.Domain.Projects;
using Microsoft.EntityFrameworkCore;

namespace AgentPlatform.Infrastructure.Persistence;

public sealed class PlatformDbContext(DbContextOptions<PlatformDbContext> options) : DbContext(options)
{
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<AgentDefinition> Agents => Set<AgentDefinition>();

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
    }
}
