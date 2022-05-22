namespace Lunet.Common;

// https://crccalc.com/
// https://github.com/force-net/Crc32.NET with Span<T> support
// TODO: Consider CRC-32C (Castagnoli)
internal static class Crc32
{
    public const int HashLength = sizeof(uint);

    public const uint CheckHash = 0x2144DF1Cu; // 0x48674BC7u for CRC-32C

    private const uint Polynomial = 0xEDB88320u; // 0x82F63B78u for CRC-32C

    private static readonly uint[] Table = new uint[16 * 256];

    static Crc32()
    {
        var table = Table;
        for (uint i = 0; i < 256; i++)
        {
            var res = i;
            for (var t = 0; t < 16; t++)
            {
                for (var k = 0; k < 8; k++)
                {
                    res = (res & 1) == 1 ? Polynomial ^ (res >> 1) : res >> 1;
                }
                table[t * 256 + i] = res;
            }
        }
    }

    /// <summary>
    /// Computes CRC-32 from multiple buffers.
    /// Call this method multiple times to chain multiple buffers.
    /// </summary>
    /// <param name="initial">
    /// Initial CRC value for the algorithm. It is zero for the first buffer.
    /// Subsequent buffers should have their initial value set to CRC value returned by previous call to this method.
    /// </param>
    /// <param name="input">Input buffer with data to be checksummed.</param>
    /// <param name="offset">Offset of the input data within the buffer.</param>
    /// <param name="length">Length of the input data in the buffer.</param>
    /// <returns>Accumulated CRC-32 of all buffers processed so far.</returns>
    public static uint Append(uint initial, byte[] input, int offset, int length)
    {
        if (input == null)
        {
            throw new ArgumentNullException(nameof(input));
        }
        if (offset < 0 || length < 0 || offset + length > input.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(length));
        }

        return AppendCore(initial, input, offset, length);
    }

    /// <summary>
    /// Computes CRC-32 from multiple buffers.
    /// Call this method multiple times to chain multiple buffers.
    /// </summary>
    /// <param name="initial">
    /// Initial CRC value for the algorithm. It is zero for the first buffer.
    /// Subsequent buffers should have their initial value set to CRC value returned by previous call to this method.
    /// </param>
    /// <param name="input">Input buffer containing data to be checksummed.</param>
    /// <returns>Accumulated CRC-32 of all buffers processed so far.</returns>
    public static uint Append(uint initial, byte[] input)
    {
        if (input == null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        return AppendCore(initial, input, 0, input.Length);
    }

    /// <summary>
    /// Computes CRC-32 from multiple buffers.
    /// Call this method multiple times to chain multiple buffers.
    /// </summary>
    /// <param name="initial">
    /// Initial CRC value for the algorithm. It is zero for the first buffer.
    /// Subsequent buffers should have their initial value set to CRC value returned by previous call to this method.
    /// </param>
    /// <param name="input">Input buffer containing data to be checksummed.</param>
    /// <returns>Accumulated CRC-32 of all buffers processed so far.</returns>
    public static uint Append(uint initial, ReadOnlySpan<byte> input)
    {
        if (input == null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        return AppendCore(initial, input);
    }

    /// <summary>
    /// Computes CRC-32 from input buffer.
    /// </summary>
    /// <param name="input">Input buffer with data to be checksummed.</param>
    /// <param name="offset">Offset of the input data within the buffer.</param>
    /// <param name="length">Length of the input data in the buffer.</param>
    /// <returns>CRC-32 of the data in the buffer.</returns>
    public static uint Compute(byte[] input, int offset, int length)
    {
        return Append(0, input, offset, length);
    }

    /// <summary>
    /// Computes CRC-32 from input buffer.
    /// </summary>
    /// <param name="input">Input buffer containing data to be checksummed.</param>
    /// <returns>CRC-32 of the buffer.</returns>
    public static uint Compute(byte[] input)
    {
        return Append(0, input);
    }

    /// <summary>
    /// Computes CRC-32 from input buffer.
    /// </summary>
    /// <param name="input">Input buffer containing data to be checksummed.</param>
    /// <returns>CRC-32 of the buffer.</returns>
    public static uint Compute(ReadOnlySpan<byte> input)
    {
        return Append(0, input);
    }

    public static bool Check(byte[] input, int offset, int length)
    {
        return CheckHash == Compute(input, offset, length);
    }

    public static bool Check(byte[] input)
    {
        return CheckHash == Compute(input, 0, input.Length);
    }

    public static bool Check(ReadOnlySpan<byte> input)
    {
        return CheckHash == Compute(input);
    }

    public static bool Check(uint initial, byte[] input, int offset, int length)
    {
        return CheckHash == Append(initial, input, offset, length);
    }

    public static bool Check(uint initial, byte[] input)
    {
        return CheckHash == Append(initial, input, 0, input.Length);
    }

    public static bool Check(uint initial, ReadOnlySpan<byte> input)
    {
        return CheckHash == Append(initial, input);
    }

    private static uint AppendCore(uint initial, byte[] input, int offset, int length)
    {
        return AppendCore(initial, new ReadOnlySpan<byte>(input, offset, length));
    }

    private static uint AppendCore(uint initial, ReadOnlySpan<byte> input)
    {
        var offset = 0;
        var length = input.Length;
        if (length <= 0)
        {
            return initial;
        }

        var hash = uint.MaxValue ^ initial;

        var table = Table;
        while (length >= 16)
        {
            var a = table[3 * 256 + input[offset + 12]]
                ^ table[2 * 256 + input[offset + 13]]
                ^ table[1 * 256 + input[offset + 14]]
                ^ table[0 * 256 + input[offset + 15]];

            var b = table[7 * 256 + input[offset + 8]]
                ^ table[6 * 256 + input[offset + 9]]
                ^ table[5 * 256 + input[offset + 10]]
                ^ table[4 * 256 + input[offset + 11]];

            var c = table[11 * 256 + input[offset + 4]]
                ^ table[10 * 256 + input[offset + 5]]
                ^ table[9 * 256 + input[offset + 6]]
                ^ table[8 * 256 + input[offset + 7]];

            var d = table[15 * 256 + ((byte)hash ^ input[offset])]
                ^ table[14 * 256 + ((byte)(hash >> 8) ^ input[offset + 1])]
                ^ table[13 * 256 + ((byte)(hash >> 16) ^ input[offset + 2])]
                ^ table[12 * 256 + ((hash >> 24) ^ input[offset + 3])];

            hash = d ^ c ^ b ^ a;
            offset += 16;
            length -= 16;
        }

        while (--length >= 0)
        {
            hash = table[(byte)(hash ^ input[offset++])] ^ (hash >> 8);
        }

        return hash ^ uint.MaxValue;
    }
}
