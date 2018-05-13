using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Bur.Net
{
    public static class AddressHelpers
    {
        public static bool ValidateAddressFamily(AddressFamily family)
        {
            return family == AddressFamily.InterNetwork || family == AddressFamily.InterNetworkV6;
        }

        public static IPAddress GetAny(AddressFamily family)
        {
            switch (family)
            {
            case AddressFamily.InterNetwork: return IPAddress.Any;
            case AddressFamily.InterNetworkV6: return IPAddress.IPv6Any;
            default: throw new ArgumentException($"Unsupported address family: {family}", nameof(family));
            }
        }

        public static IPAddress GetLoopback(AddressFamily family)
        {
            switch (family)
            {
            case AddressFamily.InterNetwork: return IPAddress.Loopback;
            case AddressFamily.InterNetworkV6: return IPAddress.IPv6Loopback;
            default: throw new ArgumentException($"Unsupported address family: {family}", nameof(family));
            }
        }
    }
}
