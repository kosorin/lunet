using System;
using System.Net;
using System.Net.Sockets;

namespace Lure.Net.Extensions
{
    internal static class AddressFamilyExtensions
    {
        public static IPEndPoint GetAnyEndPoint(this AddressFamily addressFamily)
        {
            switch (addressFamily)
            {
            case AddressFamily.InterNetwork: return new IPEndPoint(IPAddress.Any, IPEndPoint.MinPort);
            case AddressFamily.InterNetworkV6: return new IPEndPoint(IPAddress.IPv6Any, IPEndPoint.MinPort);
            default: throw new ArgumentOutOfRangeException(nameof(addressFamily), $"Address family {addressFamily} is not supported.");
            }
        }

        public static IPAddress GetAnyAddress(this AddressFamily addressFamily)
        {
            switch (addressFamily)
            {
            case AddressFamily.InterNetwork: return IPAddress.Any;
            case AddressFamily.InterNetworkV6: return IPAddress.IPv6Any;
            default: throw new ArgumentOutOfRangeException(nameof(addressFamily), $"Address family {addressFamily} is not supported.");
            }
        }

        public static IPAddress GetLoopbackAddress(this AddressFamily addressFamily)
        {
            switch (addressFamily)
            {
            case AddressFamily.InterNetwork: return IPAddress.Loopback;
            case AddressFamily.InterNetworkV6: return IPAddress.IPv6Loopback;
            default: throw new ArgumentOutOfRangeException(nameof(addressFamily), $"Address family {addressFamily} is not supported.");
            }
        }
    }
}
