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
            return addressFamily switch
            {
                AddressFamily.InterNetwork => IPVersion.IPv4,
                AddressFamily.InterNetworkV6 => IPVersion.IPv6,
                _ => throw new ArgumentOutOfRangeException(nameof(addressFamily), $"Address family {addressFamily} is not supported."),
            };
        }

        public static SystemNet_IPEndPoint GetAnyEndPoint(this AddressFamily addressFamily)
        {
            // TODO: new
            return addressFamily switch
            {
                AddressFamily.InterNetwork => new SystemNet_IPEndPoint(SystemNet_IPAddresst.Any, SystemNet_IPEndPoint.MinPort),
                AddressFamily.InterNetworkV6 => new SystemNet_IPEndPoint(SystemNet_IPAddresst.IPv6Any, SystemNet_IPEndPoint.MinPort),
                _ => throw new ArgumentOutOfRangeException(nameof(addressFamily), $"Address family {addressFamily} is not supported."),
            };
        }
    }
}
