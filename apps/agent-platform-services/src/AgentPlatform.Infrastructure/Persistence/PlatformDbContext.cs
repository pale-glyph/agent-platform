using AgentPlatform.Domain.Projects;
using Microsoft.EntityFrameworkCore;

namespace AgentPlatform.Infrastructure.Persistence;

public sealed class PlatformDbContext(DbContextOptions<PlatformDbContext> options) : DbContext(options)
{
    public DbSet<Project> Projects => Set<Project>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var project = modelBuilder.Entity<Project>();
        project.ToTable("projects");
        project.HasKey(item => item.Id);
        project.Property(item => item.Id).HasColumnName("id");
        project.Property(item => item.Name).HasColumnName("name").HasMaxLength(Project.MaximumNameLength).IsRequired();
        project.Property(item => item.CreatedAt).HasColumnName("created_at").IsRequired();
    }
}
