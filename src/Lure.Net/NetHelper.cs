using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace Lunet
{
    internal static class NetHelper
    {
        /// <summary>
        /// Gets a number of elements required to store all bits.
        /// </summary>
        /// <param name="bits">Number of bits.</param>
        /// <param name="bytes">Number of bits per element.</param>
        public static int GetElementCapacity(int bits, int bitsPerElement)
        {
            Debug.Assert(bitsPerElement > 0, $"Argument {nameof(bitsPerElement)} must be greater than 0.");

            return bits > 0
                ? ((bits - 1) / bitsPerElement) + 1
                : 0;
        }

        /// <summary>
        /// Resolves an IP address.
        /// </summary>
        /// <param name="text">IP address or hostname.</param>
        public static IPAddress ResolveAddress(string text)
        {
            IPAddress address = null;
            if (!IPAddress.TryParse(text, out address))
            {
                address = Dns
                    .GetHostAddresses(text)
                    .FirstOrDefault();
            }

            return address;
        }

        /// <summary>
        /// Resolves an IP address.
        /// </summary>
        /// <param name="text">IP address or hostname.</param>
        /// <param name="addressFamily">Expected address family.</param>
        public static IPAddress ResolveAddress(string text, AddressFamily addressFamily)
        {
            if (IPAddress.TryParse(text, out var address))
            {
                address = Dns
                    .GetHostAddresses(text)
                    .FirstOrDefault(x => x.AddressFamily == addressFamily);
                return address.AddressFamily == addressFamily
                    ? address
                    : null;
            }
            return null;
        }
    }
}
