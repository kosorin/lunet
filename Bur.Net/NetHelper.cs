using System.Diagnostics;

namespace Bur.Net
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
    }
}
