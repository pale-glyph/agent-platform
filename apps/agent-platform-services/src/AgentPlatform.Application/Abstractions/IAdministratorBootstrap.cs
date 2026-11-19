namespace AgentPlatform.Application.Abstractions;

/// <summary>Provides the deployment-controlled identities allowed to bootstrap administration.</summary>
public interface IAdministratorBootstrap
{
    bool IsBootstrapAdministrator(string clerkUserId);
}
