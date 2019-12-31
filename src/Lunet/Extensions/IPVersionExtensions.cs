using System;
using System.Net.Sockets;

namespace Lunet.Extensions
{
    internal static class IPVersionExtensions
    {
        public static AddressFamily ToAddressFamily(this IPVersion ipVersion)
        {
            return ipVersion switch
            {
                IPVersion.IPv4 => AddressFamily.InterNetwork,
                IPVersion.IPv6 => AddressFamily.InterNetworkV6,
                _ => throw new ArgumentOutOfRangeException(nameof(ipVersion), $"IP version {ipVersion} is not supported."),
            };
        }
    }
}
