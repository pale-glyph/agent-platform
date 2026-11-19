namespace AgentPlatform.Application.Abstractions;

public enum OutboundUrlPurpose
{
    Llm,
    Mcp,
    Webhook,
}

public sealed class OutboundUrlPolicyException(string message) : ArgumentException(message)
{
}

/// <summary>Validates destinations before configuration is persisted and immediately before an outbound request.</summary>
public interface IOutboundUrlPolicy
{
    Task ValidateAsync(string url, OutboundUrlPurpose purpose, CancellationToken cancellationToken);
}
