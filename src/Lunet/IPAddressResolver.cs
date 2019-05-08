using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace Lunet
{
    internal static class IPAddressResolver
    {
        /// <summary>
        /// Resolves an IP address.
        /// </summary>
        /// <param name="text">IP address or hostname.</param>
        public static IPAddress Resolve(string text)
        {
            if (IPAddress.TryParse(text, out var address))
            {
                return Dns
                    .GetHostAddresses(text)
                    .FirstOrDefault();
            }
            return null;
        }

        /// <summary>
        /// Resolves an IP address.
        /// </summary>
        /// <param name="text">IP address or hostname.</param>
        /// <param name="addressFamily">Expected address family.</param>
        public static IPAddress Resolve(string text, AddressFamily addressFamily)
        {
            if (IPAddress.TryParse(text, out var address))
            {
                return Dns
                    .GetHostAddresses(text)
                    .FirstOrDefault(x => x.AddressFamily == addressFamily);
            }
            return null;
        }
    }
}
