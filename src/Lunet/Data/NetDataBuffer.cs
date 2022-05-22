using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Lunet.Data;

public abstract class NetDataBuffer
{
    private byte[] _data;

    protected NetDataBuffer(int length)
    {
        if (length < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(length), "Length must be greater or equal to 0.");
        }

        IsDataOwner = true;

        _data = new byte[length];
        DataOffset = 0;
        DataLength = length;
    }

    protected NetDataBuffer(byte[] data, int offset, int length)
    {
        if (offset + length > data.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(length), "Offset + length must not be greater than data length.");
        }

        IsDataOwner = false;

        _data = data;
        DataOffset = offset;
        DataLength = length;
    }

    public bool IsDataOwner { get; }

    public byte[] Data => _data;

    public int DataOffset { get; }

    public int DataLength { get; private set; }

    public abstract int Offset { get; }

    public abstract int Length { get; }

    public abstract int Position { get; }

    public ReadOnlySpan<byte> GetDataReadOnlySpan()
    {
        return new ReadOnlySpan<byte>(Data, DataOffset, DataLength);
    }

    public Memory<byte> GetDataMemory()
    {
        return new Memory<byte>(Data, DataOffset, DataLength);
    }

    public virtual byte[] GetBytes()
    {
        return GetReadOnlySpan().ToArray();
    }

    public virtual ReadOnlySpan<byte> GetReadOnlySpan()
    {
        return new ReadOnlySpan<byte>(Data, Offset, Length);
    }

    public virtual Memory<byte> GetMemory()
    {
        return new Memory<byte>(Data, Offset, Length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void EnsureSize(int length)
    {
        if (!IsDataOwner)
        {
            if (DataLength < length)
            {
                throw new InvalidOperationException();
            }
            return;
        }

        Debug.Assert(DataOffset == 0);

        if (DataLength < length)
        {
            DataLength = RoundUpPowerOf2(length);
            Array.Resize(ref _data, DataLength);
        }
        else if (_data == null)
        {
            DataLength = RoundUpPowerOf2(length);
            _data = new byte[DataLength];
        }
    }

    /// <summary>
    /// Round up to the next highest power of 2.
    /// </summary>
    /// <param name="value"></param>
    /// <returns>
    /// In 12 operations, this code computes the next highest power of 2 for a 32-bit integer.
    /// Maximum value is 1,073,741,824 (1 GB).
    /// Source: http://graphics.stanford.edu/~seander/bithacks.html#RoundUpPowerOf2
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int RoundUpPowerOf2(int value)
    {
        value--;
        value |= value >> 1;
        value |= value >> 2;
        value |= value >> 4;
        value |= value >> 8;
        value |= value >> 16;
        value++;

        return value;
    }

    [StructLayout(LayoutKind.Explicit)]
    private protected struct FloatingPointConverter
    {
        [FieldOffset(0)]
        public float Float;

        [FieldOffset(0)]
        public double Double;

        [FieldOffset(0)]
        public uint UInt;

        [FieldOffset(0)]
        public ulong ULong;


        [FieldOffset(0)]
        public byte Byte0;

        [FieldOffset(1)]
        public byte Byte1;

        [FieldOffset(2)]
        public byte Byte2;

        [FieldOffset(3)]
        public byte Byte3;

        [FieldOffset(4)]
        public byte Byte4;

        [FieldOffset(5)]
        public byte Byte5;

        [FieldOffset(6)]
        public byte Byte6;

        [FieldOffset(7)]
        public byte Byte7;
    }
}
