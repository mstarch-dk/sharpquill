using System.Net;
using System.Net.Sockets;

namespace SharpQuill.Application.LinkPreviews.Services;

/// <summary>
/// Builds the HTTP handler used to fetch user-supplied URLs. Every outgoing connection, including those
/// made while following redirects, is checked at connect time against the resolved IP addresses, so
/// requests cannot reach loopback, private networks or cloud metadata endpoints (also not via DNS rebinding).
/// </summary>
public static class PublicAddressHttpHandler
{
    public const int MaxRedirects = 5;

    public static SocketsHttpHandler Create(Func<IPAddress, bool>? isAllowedAddress = null)
    {
        var isAllowed = isAllowedAddress ?? IsPublicAddress;

        return new SocketsHttpHandler
        {
            UseProxy = false,
            AllowAutoRedirect = true,
            MaxAutomaticRedirections = MaxRedirects,
            ConnectCallback = async (context, cancellationToken) =>
            {
                var host = context.DnsEndPoint.Host;
                var addresses = IPAddress.TryParse(host, out var literal)
                    ? [literal]
                    : await Dns.GetHostAddressesAsync(host, cancellationToken);

                // Refuse if any resolved address is non-public, rather than picking a "good" one
                if (addresses.Length == 0 || !addresses.All(isAllowed))
                {
                    throw new HttpRequestException($"Refusing to connect to '{host}': it does not resolve to a public address");
                }

                var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
                try
                {
                    await socket.ConnectAsync(addresses, context.DnsEndPoint.Port, cancellationToken);
                    return new NetworkStream(socket, ownsSocket: true);
                }
                catch
                {
                    socket.Dispose();
                    throw;
                }
            }
        };
    }

    /// <summary>
    /// True only for globally routable unicast addresses.
    /// </summary>
    public static bool IsPublicAddress(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        var b = address.GetAddressBytes();

        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            return !(b[0] == 0                                         // 0.0.0.0/8 "this network"
                || b[0] == 10                                          // 10.0.0.0/8 private
                || (b[0] == 100 && b[1] >= 64 && b[1] <= 127)          // 100.64.0.0/10 carrier-grade NAT
                || b[0] == 127                                         // 127.0.0.0/8 loopback
                || (b[0] == 169 && b[1] == 254)                        // 169.254.0.0/16 link-local, cloud metadata
                || (b[0] == 172 && b[1] >= 16 && b[1] <= 31)           // 172.16.0.0/12 private
                || (b[0] == 192 && b[1] == 0 && b[2] == 0)             // 192.0.0.0/24 IETF protocol assignments
                || (b[0] == 192 && b[1] == 0 && b[2] == 2)             // 192.0.2.0/24 documentation
                || (b[0] == 192 && b[1] == 168)                        // 192.168.0.0/16 private
                || (b[0] == 198 && (b[1] == 18 || b[1] == 19))         // 198.18.0.0/15 benchmarking
                || (b[0] == 198 && b[1] == 51 && b[2] == 100)          // 198.51.100.0/24 documentation
                || (b[0] == 203 && b[1] == 0 && b[2] == 113)           // 203.0.113.0/24 documentation
                || b[0] >= 224);                                       // multicast, reserved, broadcast
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            if (b.AsSpan(0, 12).IndexOfAnyExcept((byte)0) < 0)
                return false;                                          // ::, ::1 and IPv4-compatible addresses
            if ((b[0] & 0xFE) == 0xFC)
                return false;                                          // fc00::/7 unique local
            if (b[0] == 0xFE && (b[1] & 0x80) == 0x80)
                return false;                                          // fe80::/10 link-local, fec0::/10 site-local
            if (b[0] == 0xFF)
                return false;                                          // ff00::/8 multicast
            if (b[0] == 0x00 && b[1] == 0x64 && b[2] == 0xFF && b[3] == 0x9B)
                return IsPublicAddress(new IPAddress(b[12..16]));      // 64:ff9b::/96 NAT64 embeds IPv4
            if (b[0] == 0x20 && b[1] == 0x02)
                return IsPublicAddress(new IPAddress(b[2..6]));        // 2002::/16 6to4 embeds IPv4
            return true;
        }

        return false;
    }
}
