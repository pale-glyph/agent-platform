using Microsoft.EntityFrameworkCore;

namespace Tenancy.Api;

public sealed class TenancyDb(DbContextOptions<TenancyDb> options) : DbContext(options)
{
    public DbSet<AccountRow> Accounts => Set<AccountRow>();
    public DbSet<OrganisationRow> Organisations => Set<OrganisationRow>();
    public DbSet<MemberRow> Members => Set<MemberRow>();
    public DbSet<AuditRow> Audit => Set<AuditRow>();
    public DbSet<OutboxRow> Outbox => Set<OutboxRow>();
    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<AccountRow>().ToTable("accounts").HasIndex(x => x.ClerkUserId).IsUnique();
        model.Entity<OrganisationRow>().ToTable("organisations").Property(x => x.Version).IsConcurrencyToken();
        model.Entity<MemberRow>().ToTable("organisation_members");
        model.Entity<AuditRow>().ToTable("organisation_audit_events");
        model.Entity<AuditRow>().HasOne<OrganisationRow>().WithMany().HasForeignKey(x => x.OrganisationId);
        model.Entity<OutboxRow>().ToTable("outbox_messages");
    }
}

public sealed class AccountRow
{
    public Guid Id { get; set; }
    public required string ClerkUserId { get; set; }
    public bool IsPlatformAdministrator { get; set; }
    public string Status { get; set; } = "Active";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset LastSeenAt { get; set; }
}
public sealed class OrganisationRow
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public Guid OwnerAccountId { get; set; }
    public string Status { get; set; } = "Active";
    public long AccessRevision { get; set; }
    public long Version { get; set; } = 1;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
public sealed class MemberRow
{
    public Guid Id { get; set; }
    public Guid OrganisationId { get; set; }
    public Guid AccountId { get; set; }
    public string Status { get; set; } = "Active";
    public Guid GrantedByAccountId { get; set; }
    public DateTimeOffset GrantedAt { get; set; }
    public Guid? RevokedByAccountId { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
}
public sealed class AuditRow
{
    public Guid Id { get; set; }
    public Guid OrganisationId { get; set; }
    public required string EventType { get; set; }
    public Guid ActorAccountId { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
}
public sealed class OutboxRow
{
    public Guid Id { get; set; }
    public required string EventType { get; set; }
    public required string Subject { get; set; }
    public required string Payload { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
}
