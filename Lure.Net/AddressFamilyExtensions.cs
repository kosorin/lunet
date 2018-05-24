using System;
using System.Net;
using System.Net.Sockets;

namespace Lure.Net
{
    internal static class AddressFamilyExtensions
    {
        public static IPEndPoint GetAnyEndPoint(this AddressFamily family)
        {
            switch (family)
            {
            case AddressFamily.InterNetwork: return new IPEndPoint(IPAddress.Any, IPEndPoint.MinPort);
            case AddressFamily.InterNetworkV6: return new IPEndPoint(IPAddress.IPv6Any, IPEndPoint.MinPort);
            default: throw new ArgumentOutOfRangeException(nameof(family), $"Not supported address family: {family}");
            }
        }

        public static IPAddress GetAnyAddress(this AddressFamily addressFamily)
        {
            switch (addressFamily)
            {
            case AddressFamily.InterNetwork: return IPAddress.Any;
            case AddressFamily.InterNetworkV6: return IPAddress.IPv6Any;
            default: throw new ArgumentOutOfRangeException(nameof(addressFamily), $"Not supported address family: {addressFamily}");
            }
        }

        public static IPAddress GetLoopbackAddress(this AddressFamily addressFamily)
        {
            switch (addressFamily)
            {
            case AddressFamily.InterNetwork: return IPAddress.Loopback;
            case AddressFamily.InterNetworkV6: return IPAddress.IPv6Loopback;
            default: throw new ArgumentOutOfRangeException(nameof(addressFamily), $"Not supported address family: {addressFamily}");
            }
        }
    }
}
