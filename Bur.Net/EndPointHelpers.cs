using System;
using System.Net;
using System.Net.Sockets;

namespace Bur.Net
{
    public static class EndPointHelpers
    {
        public const int AnyPort = IPEndPoint.MinPort;

        public static bool ValidatePortNumber(int port)
        {
            return port >= IPEndPoint.MinPort && port <= IPEndPoint.MaxPort;
        }

        public static IPEndPoint GetAny(AddressFamily family)
        {
            switch (family)
            {
            case AddressFamily.InterNetwork: return new IPEndPoint(IPAddress.Any, AnyPort);
            case AddressFamily.InterNetworkV6: return new IPEndPoint(IPAddress.IPv6Any, AnyPort);
            default: throw new ArgumentException($"Unsupported address family: {family}", nameof(family));
            }
        }
    }
}
