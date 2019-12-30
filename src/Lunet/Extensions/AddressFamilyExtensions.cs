using System;
using System.Net.Sockets;
using SystemNet_IPAddresst = System.Net.IPAddress;
using SystemNet_IPEndPoint = System.Net.IPEndPoint;

namespace Lunet.Extensions
{
    internal static class AddressFamilyExtensions
    {
        public static IPVersion ToIPVersion(this AddressFamily addressFamily)
        {
            switch (addressFamily)
            {
            case AddressFamily.InterNetwork: return IPVersion.IPv4;
            case AddressFamily.InterNetworkV6: return IPVersion.IPv6;
            default: throw new ArgumentOutOfRangeException(nameof(addressFamily), $"Address family {addressFamily} is not supported.");
            }
        }

        public static SystemNet_IPEndPoint GetAnyEndPoint(this AddressFamily addressFamily)
        {
            switch (addressFamily)
            {
            case AddressFamily.InterNetwork: return new SystemNet_IPEndPoint(SystemNet_IPAddresst.Any, SystemNet_IPEndPoint.MinPort);
            case AddressFamily.InterNetworkV6: return new SystemNet_IPEndPoint(SystemNet_IPAddresst.IPv6Any, SystemNet_IPEndPoint.MinPort);
            default: throw new ArgumentOutOfRangeException(nameof(addressFamily), $"Address family {addressFamily} is not supported.");
            }
        }
    }
}
