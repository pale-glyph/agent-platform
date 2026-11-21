namespace Tenancy.Domain;

public sealed class Organisation
{
    private readonly HashSet<Guid> members = [];

    public Organisation(Guid id, Guid ownerAccountId, string name, string? description = null)
    {
        if (id == Guid.Empty || ownerAccountId == Guid.Empty)
            throw new ArgumentException("Organisation and owner IDs are required.");
        Id = id;
        OwnerAccountId = ownerAccountId;
        SetMetadata(name, description);
    }

    public Guid Id { get; }
    public Guid OwnerAccountId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public OrganisationStatus Status { get; private set; } = OrganisationStatus.Active;
    public bool IsDeleted => Status == OrganisationStatus.Deleted;
    public long AccessRevision { get; private set; }
    public IReadOnlySet<Guid> Members => members;

    public void Update(Guid actor, string name, string? description)
    {
        RequireOwner(actor);
        SetMetadata(name, description);
    }

    public void Grant(Guid actor, Guid accountId)
    {
        RequireOwner(actor);
        if (accountId == Guid.Empty || accountId == OwnerAccountId)
            throw new ArgumentException("A non-owner account is required.", nameof(accountId));
        if (!members.Add(accountId)) throw new InvalidOperationException("Account is already a member.");
        AccessRevision++;
    }

    public void Revoke(Guid actor, Guid accountId)
    {
        RequireOwner(actor);
        if (!members.Remove(accountId)) throw new InvalidOperationException("Account is not a member.");
        AccessRevision++;
    }

    public void Transfer(Guid actor, Guid accountId)
    {
        RequireOwner(actor);
        if (!members.Remove(accountId)) throw new InvalidOperationException("The new owner must be a member.");
        members.Add(OwnerAccountId);
        OwnerAccountId = accountId;
        AccessRevision++;
    }

    public void Delete(Guid actor)
    {
        RequireOwner(actor);
        BeginDeletion();
    }

    // Account deactivation must put every owned organisation into this state in
    // the same transaction that deactivates the account, unless ownership is transferred.
    public void BeginDeletion()
    {
        if (Status != OrganisationStatus.Active) throw new InvalidOperationException("Organisation is not active.");
        Status = OrganisationStatus.Deleting;
        members.Clear();
        AccessRevision++;
    }

    public void CompleteDeletion()
    {
        if (Status != OrganisationStatus.Deleting) throw new InvalidOperationException("Organisation is not deleting.");
        Status = OrganisationStatus.Deleted;
    }

    public OrganisationRole GetRole(Guid accountId) => Status != OrganisationStatus.Active ? OrganisationRole.None :
        accountId == OwnerAccountId ? OrganisationRole.Owner :
        members.Contains(accountId) ? OrganisationRole.Member : OrganisationRole.None;

    private void RequireOwner(Guid actor)
    {
        if (Status != OrganisationStatus.Active || actor != OwnerAccountId) throw new InvalidOperationException("An active organisation owner is required.");
    }

    private void SetMetadata(string name, string? description)
    {
        var trimmed = name?.Trim();
        if (string.IsNullOrEmpty(trimmed) || trimmed.Length > 256)
            throw new ArgumentException("Name must contain 1 to 256 characters.", nameof(name));
        if (description?.Length > 4000) throw new ArgumentException("Description exceeds 4000 characters.", nameof(description));
        Name = trimmed;
        Description = description;
    }
}

public enum OrganisationRole { None, Member, Owner }
public enum OrganisationStatus { Active, Deleting, Deleted }
