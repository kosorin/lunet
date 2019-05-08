using System;
using System.Net.Sockets;

namespace Lunet.Extensions
{
    internal static class IPVersionExtensions
    {
        public static AddressFamily ToAddressFamily(this IPVersion ipVersion)
        {
            switch (ipVersion)
            {
            case IPVersion.IPv4: return AddressFamily.InterNetwork;
            case IPVersion.IPv6: return AddressFamily.InterNetworkV6;
            default: throw new ArgumentOutOfRangeException(nameof(ipVersion), $"IP version {ipVersion} is not supported.");
            }
        }
    }
}
