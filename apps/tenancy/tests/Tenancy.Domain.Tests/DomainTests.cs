using Tenancy.Domain;
using Xunit;

namespace Tenancy.Domain.Tests;

public class DomainTests
{
    [Fact]
    public void TransferMakesMemberOwnerAndRetainsFormerOwner()
    {
        var owner = Guid.NewGuid();
        var member = Guid.NewGuid();
        var organisation = new Organisation(Guid.NewGuid(), owner, "  Example  ");
        organisation.Grant(owner, member);
        organisation.Transfer(owner, member);
        Assert.Equal("Example", organisation.Name);
        Assert.Equal(OrganisationRole.Owner, organisation.GetRole(member));
        Assert.Equal(OrganisationRole.Member, organisation.GetRole(owner));
        Assert.DoesNotContain(member, organisation.Members);
        Assert.Equal(2, organisation.AccessRevision);
        Assert.Throws<InvalidOperationException>(() => organisation.Grant(owner, Guid.NewGuid()));
    }

    [Fact]
    public void DeletionImmediatelyRemovesAccessAndRequiresCompletion()
    {
        var owner = Guid.NewGuid();
        var member = Guid.NewGuid();
        var organisation = new Organisation(Guid.NewGuid(), owner, "Example");
        organisation.Grant(owner, member);
        organisation.Delete(owner);
        Assert.Equal(OrganisationStatus.Deleting, organisation.Status);
        Assert.Equal(OrganisationRole.None, organisation.GetRole(owner));
        Assert.Equal(OrganisationRole.None, organisation.GetRole(member));
        Assert.Throws<InvalidOperationException>(() => organisation.Delete(owner));
        organisation.CompleteDeletion();
        Assert.Equal(OrganisationStatus.Deleted, organisation.Status);
    }

    [Fact]
    public void LastAdministratorCannotBeRemovedOrDeactivated()
    {
        var account = new Account(Guid.NewGuid(), " user_1 ", true);
        Assert.Equal("user_1", account.ClerkUserId);
        Assert.Throws<InvalidOperationException>(() => account.SetAdministrator(false, 1));
        Assert.Throws<InvalidOperationException>(() => account.Deactivate(AccountStatus.Deleted, 1));
        account.Deactivate(AccountStatus.Deleted, 2);
        Assert.Throws<InvalidOperationException>(() => account.Deactivate(AccountStatus.Blocked, 2));
    }
}
