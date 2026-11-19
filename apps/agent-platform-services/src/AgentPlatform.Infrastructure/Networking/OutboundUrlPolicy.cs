using System.Net;
using System.Net.Sockets;
using AgentPlatform.Application.Abstractions;
using Microsoft.Extensions.Configuration;

namespace AgentPlatform.Infrastructure.Networking;

/// <summary>Allowlist-based egress policy. It rejects unconfigured, non-HTTPS, and private-network destinations.</summary>
public sealed class OutboundUrlPolicy(IConfiguration configuration) : IOutboundUrlPolicy
{
    public async Task ValidateAsync(string url, OutboundUrlPurpose purpose, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || !string.IsNullOrEmpty(uri.UserInfo) || uri.Port is 0)
            throw new OutboundUrlPolicyException("Outbound URLs must be absolute URLs without user information.");

        var allowedHosts = ReadAllowedHosts(purpose);
        if (!allowedHosts.Contains(uri.Host, StringComparer.OrdinalIgnoreCase))
            throw new OutboundUrlPolicyException($"The {purpose.ToString().ToLowerInvariant()} URL host is not allowlisted.");

        if (uri.Scheme != Uri.UriSchemeHttps)
        {
            if (purpose != OutboundUrlPurpose.Mcp || uri.Scheme != Uri.UriSchemeHttp || !IsExplicitInternalMcpHost(uri.Host))
                throw new OutboundUrlPolicyException("Outbound URLs must use HTTPS. HTTP is permitted only for explicitly allowlisted internal MCP hosts.");
        }

        if (IsExplicitInternalMcpHost(uri.Host)) return;

        IPAddress[] addresses;
        try
        {
            addresses = IPAddress.TryParse(uri.Host, out var address)
                ? [address]
                : await Dns.GetHostAddressesAsync(uri.DnsSafeHost, cancellationToken);
        }
        catch (SocketException)
        {
            throw new OutboundUrlPolicyException("The outbound URL host could not be resolved.");
        }
        if (addresses.Length == 0 || addresses.Any(IsPrivateOrReserved))
            throw new OutboundUrlPolicyException("Outbound URL hosts must resolve only to public IP addresses.");
    }

    private string[] ReadAllowedHosts(OutboundUrlPurpose purpose) =>
        (configuration[$"OutboundHttp:Allowed{purpose}Hosts"] ?? string.Empty)
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

    private bool IsExplicitInternalMcpHost(string host) =>
        ReadAllowedHosts(OutboundUrlPurpose.Mcp).Contains(host, StringComparer.OrdinalIgnoreCase) &&
        (configuration["OutboundHttp:InternalMcpHosts"] ?? string.Empty)
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Contains(host, StringComparer.OrdinalIgnoreCase);

    private static bool IsPrivateOrReserved(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6) address = address.MapToIPv4();
        if (IPAddress.IsLoopback(address) || address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any) || address.Equals(IPAddress.IPv6Loopback)) return true;
        var bytes = address.MapToIPv4().GetAddressBytes();
        if (address.AddressFamily == AddressFamily.InterNetwork)
            return bytes[0] == 10 || bytes[0] == 127 || bytes[0] == 0 ||
                   (bytes[0] == 169 && bytes[1] == 254) ||
                   (bytes[0] == 172 && bytes[1] is >= 16 and <= 31) ||
                   (bytes[0] == 192 && bytes[1] == 168) ||
                   (bytes[0] == 100 && bytes[1] is >= 64 and <= 127) ||
                   bytes[0] >= 224;

        return address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || address.IsIPv6Multicast || address.IsIPv6Teredo ||
               (address.AddressFamily == AddressFamily.InterNetworkV6 && (address.GetAddressBytes()[0] & 0xfe) == 0xfc);
    }
}
