using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using Lunet.Common;

namespace Lunet.Data;

/// <summary>
/// Fixed size vector of booleans.
/// </summary>
[SuppressMessage("Style", "IDE0007:Use implicit type", Justification = "<Pending>")]
public sealed class BitVector : IEquatable<BitVector>
{
    public static readonly BitVector Empty = new BitVector(0);

    /// <summary>
    /// Initializes a new instance of the <see cref="BitVector" /> class with capacity for one byte.
    /// </summary>
    public BitVector()
        : this(NC.BitsPerByte, false)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BitVector" /> class.
    /// </summary>
    /// <param name="capacity">Number of bits.</param>
    public BitVector(int capacity)
        : this(capacity, false)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BitVector" /> class with default bit values.
    /// </summary>
    /// <param name="capacity">Number of bits.</param>
    /// <param name="defaultValue">Value to assign to each bit.</param>
    public BitVector(int capacity, bool defaultValue)
    {
        if (capacity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity));
        }

        Capacity = capacity;
        ByteCapacity = NetHelper.GetElementCapacity(capacity, NC.BitsPerByte);

        Data = new int[NetHelper.GetElementCapacity(capacity, NC.BitsPerInt)];

        SetAll(defaultValue);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BitVector" /> class with bytes.
    /// </summary>
    /// <param name="bytes">Source bytes.</param>
    public BitVector(byte[] bytes)
        : this(bytes, bytes.Length * NC.BitsPerByte)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BitVector" /> class with bytes.
    /// </summary>
    /// <param name="bytes">Source bytes.</param>
    /// <param name="capacity">Number of bits.</param>
    public BitVector(byte[] bytes, int capacity)
        : this(new ReadOnlySpan<byte>(bytes), capacity)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BitVector" /> class with bytes.
    /// </summary>
    /// <param name="bytes">Source bytes.</param>
    /// <param name="capacity">Number of bits.</param>
    public BitVector(ReadOnlySpan<byte> bytes, int capacity)
    {
        if (bytes == null)
        {
            throw new ArgumentNullException(nameof(bytes));
        }
        if (capacity < 0 || capacity <= (bytes.Length - 1) * NC.BitsPerByte || capacity > bytes.Length * NC.BitsPerByte)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity));
        }

        Capacity = capacity;
        ByteCapacity = NetHelper.GetElementCapacity(capacity, NC.BitsPerByte);

        Data = new int[NetHelper.GetElementCapacity(capacity, NC.BitsPerInt)];

        for (int i = 0, b = 0; i < Data.Length; i++)
        {
            for (var j = 0; j < sizeof(int); j++)
            {
                Data[i] |= bytes[b] << (j * NC.BitsPerByte);
                if (++b >= ByteCapacity)
                {
                    goto End;
                }
            }
        }
    End: ;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BitVector" /> class with another bit vector.
    /// </summary>
    /// <param name="source">Source bit vector.</param>
    /// <param name="offset"></param>
    /// <param name="count"></param>
    internal BitVector(BitVector source, int offset, int count)
    {
        if (offset != 0)
        {
            throw new NotSupportedException();
        }

        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }
        if (count < 0 || count > source.Capacity)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }
        if (offset < 0 || offset + count > source.Capacity)
        {
            throw new ArgumentOutOfRangeException(nameof(offset));
        }

        Capacity = count;
        ByteCapacity = NetHelper.GetElementCapacity(Capacity, NC.BitsPerByte);

        Data = new int[NetHelper.GetElementCapacity(Capacity, NC.BitsPerInt)];

        Array.Copy(source.Data, 0, Data, 0, Data.Length);
    }


    /// <summary>
    /// Gets the number of bits stored in this vector.
    /// </summary>
    public int Capacity { get; }

    /// <summary>
    /// Gets the number of bytes to store all bits.
    /// </summary>
    public int ByteCapacity { get; }

    internal int[] Data { get; }


    /// <summary>
    /// Gets the bit at the specified index.
    /// </summary>
    [IndexerName("Bit")]
    public bool this[int bitIndex]
    {
        get => Get(bitIndex);
        set => Set(bitIndex, value);
    }


    /// <summary>
    /// Shift all the bits to left on count bits.
    /// </summary>
    public void LeftShift(int count)
    {
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }
        if (count == 0)
        {
            return;
        }

        int lengthToClear;
        if (count < Capacity)
        {
            lengthToClear = count / NC.BitsPerInt;

            var lastIndex = (Capacity - 1) / NC.BitsPerInt;
            var shiftCount = count - lengthToClear * NC.BitsPerInt;

            if (shiftCount == 0)
            {
                Array.Copy(Data, 0, Data, lengthToClear, lastIndex + 1 - lengthToClear);
            }
            else
            {
                var fromIndex = lastIndex - lengthToClear;
                unchecked
                {
                    while (fromIndex > 0)
                    {
                        var left = Data[fromIndex] << shiftCount;
                        var right = (uint)Data[--fromIndex] >> (NC.BitsPerInt - shiftCount);
                        Data[lastIndex] = left | (int)right;
                        lastIndex--;
                    }
                    Data[lastIndex] = Data[fromIndex] << shiftCount;
                }
            }
        }
        else
        {
            lengthToClear = Data.Length;
        }

        if (lengthToClear > 0)
        {
            Array.Clear(Data, 0, lengthToClear);
        }
    }

    /// <summary>
    /// Shift all the bits to right on count bits.
    /// </summary>
    public void RightShift(int count)
    {
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }
        if (count == 0)
        {
            return;
        }

        var clearToIndex = 0;
        var length = Data.Length;
        if (count < Capacity)
        {
            var fromIndex = count / NC.BitsPerInt;
            var shiftCount = count - fromIndex * NC.BitsPerInt;

            if (shiftCount == 0)
            {
                unchecked
                {
                    var mask = uint.MaxValue >> (NC.BitsPerInt - Capacity % NC.BitsPerInt);
                    Data[length - 1] &= (int)mask;
                }
                Array.Copy(Data, fromIndex, Data, 0, length - fromIndex);
                clearToIndex = length - fromIndex;
            }
            else
            {
                var lastIndex = length - 1;
                unchecked
                {
                    while (fromIndex < lastIndex)
                    {
                        var right = (uint)Data[fromIndex] >> shiftCount;
                        var left = Data[++fromIndex] << (NC.BitsPerInt - shiftCount);
                        Data[clearToIndex++] = left | (int)right;
                    }
                    var mask = uint.MaxValue >> (NC.BitsPerInt - Capacity % NC.BitsPerInt);
                    mask &= (uint)Data[fromIndex];
                    Data[clearToIndex++] = (int)(mask >> shiftCount);
                }
            }
        }

        var lengthToClear = length - clearToIndex;
        if (lengthToClear > 0)
        {
            Array.Clear(Data, clearToIndex, lengthToClear);
        }
    }


    /// <summary>
    /// Rotate all bits to right.
    /// </summary>
    public void RightRotate()
    {
        var lengthMinusOne = Data.Length - 1;

        var firstBit = Data[0] & NC.One;
        for (var i = 0; i < lengthMinusOne; i++)
        {
            Data[i] = ((Data[i] >> 1) & ~(1 << (NC.BitsPerInt - 1))) | (Data[i + 1] << (NC.BitsPerInt - 1));
        }

        var lastIndex = Capacity - 1 - NC.BitsPerInt * lengthMinusOne;

        var last = Data[lengthMinusOne];
        last >>= 1;
        last |= firstBit << lastIndex;

        Data[lengthMinusOne] = last;
    }


    /// <summary>
    /// Gets the first (lowest) index set to true.
    /// </summary>
    public int GetFirstSetBitIndex()
    {
        var byteIndex = 0;

        var @byte = Data[0];
        while (@byte == 0)
        {
            byteIndex++;
            @byte = Data[byteIndex];
        }

        var bitIndex = 0;
        while (((@byte >> bitIndex) & NC.One) == 0)
        {
            bitIndex++;
        }

        return byteIndex * NC.BitsPerInt + bitIndex;
    }

    /// <summary>
    /// Gets the number of set bits in vector.
    /// </summary>
    public int GetNumberOfSetBits()
    {
        var count = 0;
        for (var i = 0; i < Data.Length; i++)
        {
            var x = Data[i];
            x -= (x >> 1) & 0x55555555;
            x = (x & 0x33333333) + ((x >> 2) & 0x33333333);
            x = (((x + (x >> 4)) & 0x0F0F0F0F) * 0x01010101) >> 24;
            count += x;
        }
        return count;
    }

    /// <summary>
    /// Gets the number of clear bits in vector.
    /// </summary>
    public int GetNumberOfClearBits()
    {
        return Capacity - GetNumberOfSetBits();
    }

    /// <summary>
    /// Checks whether all bits are cleared.
    /// </summary>
    public bool IsEmpty()
    {
        return GetNumberOfSetBits() == 0;
    }

    /// <summary>
    /// Checks whether all bits are set.
    /// </summary>
    public bool IsFull()
    {
        return GetNumberOfSetBits() == Capacity;
    }


    /// <summary>
    /// Gets the bit at the specified index.
    /// </summary>
    public bool Get(int index)
    {
        if (index < 0 || index >= Capacity)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return (Data[index / NC.BitsPerInt] & GetBit(index)) != 0;
    }

    /// <summary>
    /// Sets or clears the bit at the specified index.
    /// </summary>
    public void Set(int index, bool value)
    {
        if (index < 0 || index >= Capacity)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        if (value)
        {
            Data[index / NC.BitsPerInt] |= GetBit(index);
        }
        else
        {
            Data[index / NC.BitsPerInt] &= ~GetBit(index);
        }
    }


    /// <summary>
    /// Sets the bit at the specified index.
    /// </summary>
    public void Set(int index)
    {
        Set(index, true);
    }

    /// <summary>
    /// Sets all bits.
    /// </summary>
    public void SetAll()
    {
        SetAll(true);
    }

    /// <summary>
    /// Sets all bits to the value.
    /// </summary>
    public void SetAll(bool value)
    {
        var fillValue = value ? unchecked((int)NC.Int) : NC.Zero;
        for (var i = 0; i < Data.Length; i++)
        {
            Data[i] = fillValue;
        }
    }

    /// <summary>
    /// Sets bits at the specified indexes.
    /// </summary>
    public void SetAll(params int[] indexes)
    {
        for (var i = 0; i < indexes.Length; i++)
        {
            Set(indexes[i], true);
        }
    }


    /// <summary>
    /// Clears the bit at the specified index.
    /// </summary>
    public void Clear(int index)
    {
        Set(index, false);
    }

    /// <summary>
    /// Clears all bits.
    /// </summary>
    public void ClearAll()
    {
        SetAll(false);
    }

    /// <summary>
    /// Clears bits at the specified indexes.
    /// </summary>
    public void ClearAll(params int[] indexes)
    {
        for (var i = 0; i < indexes.Length; i++)
        {
            Set(indexes[i], false);
        }
    }


    /// <summary>
    /// Returns a string that represents this object.
    /// </summary>
    public override string ToString()
    {
        var sb = new StringBuilder(Capacity + 2);
        sb.Append('[');

        const int lastOne = NC.One << (NC.BitsPerInt - 1);
        for (int i = Data.Length - 1, b = Capacity; i >= 0; i--)
        {
            var startBitIndex = (NC.BitsPerInt - b % NC.BitsPerInt) % NC.BitsPerInt;
            var data = Data[i] << startBitIndex;
            for (var j = startBitIndex; j < NC.BitsPerInt; j++)
            {
                var bit = (data & lastOne) == lastOne;
                sb.Append(bit ? '1' : '0');
                if (--b <= 0)
                {
                    goto End;
                }
                data <<= 1;
            }
        }

    End:
        sb.Append(']');
        return sb.ToString();
    }

    /// <summary>
    /// Returns a byte array.
    /// </summary>
    public byte[] ToBytes()
    {
        return AsBytes().ToArray();
    }

    /// <summary>
    /// Returns a bit array.
    /// </summary>
    public bool[] ToBits()
    {
        return AsBits().ToArray();
    }

    public IEnumerable<byte> AsBytes()
    {
        for (int i = 0, b = 0; i < Data.Length; i++)
        {
            var data = Data[i];
            for (var j = 0; j < sizeof(int); j++)
            {
                var @byte = (byte)((data >> (j * NC.BitsPerByte)) & NC.Byte);
                yield return @byte;
                if (++b >= ByteCapacity)
                {
                    yield break;
                }
            }
        }
    }

    public IEnumerable<bool> AsBits()
    {
        for (int i = 0, b = 0; i < Data.Length; i++)
        {
            var data = Data[i];
            for (var j = 0; j < sizeof(int) * NC.BitsPerByte; j++)
            {
                var bit = (data & NC.One) == NC.One;
                yield return bit;
                if (++b >= Capacity)
                {
                    yield break;
                }
                data >>= 1;
            }
        }
    }

    public BitVector Clone()
    {
        return new BitVector(this, 0, Capacity);
    }

    public BitVector Clone(int offset, int count)
    {
        return new BitVector(this, offset, count);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetBit(int index)
    {
        return NC.One << (index % NC.BitsPerInt);
    }


    #region IEquatable

    private static readonly ArrayEqualityComparer<int> DataComparer = new ArrayEqualityComparer<int>();

    public static bool operator ==(BitVector left, BitVector right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }
        if (left is null)
        {
            return false;
        }
        if (right is null)
        {
            return false;
        }
        return left.EqualsCore(right);
    }

    public static bool operator !=(BitVector left, BitVector right)
    {
        return !(left == right);
    }

    public bool Equals(BitVector? other)
    {
        if (ReferenceEquals(this, other))
        {
            return true;
        }
        if (other is null)
        {
            return false;
        }
        return EqualsCore(other);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj))
        {
            return true;
        }
        return obj is BitVector other && EqualsCore(other);
    }

    public override int GetHashCode()
    {
        return DataComparer.GetHashCode(Data);
    }

    private bool EqualsCore(BitVector other)
    {
        if (Capacity != other.Capacity)
        {
            return false;
        }
        return DataComparer.Equals(Data, other.Data);
    }

    #endregion IEquatable

    #region Explicit operators

    public static explicit operator sbyte(BitVector vector)
    {
        Debug.Assert(vector.ByteCapacity <= sizeof(sbyte));

        if (vector.Data.Length == 0)
        {
            return default;
        }
        return (sbyte)(vector.Data[0] & NC.Byte);
    }

    public static explicit operator byte(BitVector vector)
    {
        Debug.Assert(vector.ByteCapacity <= sizeof(byte));

        if (vector.Data.Length == 0)
        {
            return default;
        }
        return (byte)(vector.Data[0] & NC.Byte);
    }

    public static explicit operator short(BitVector vector)
    {
        Debug.Assert(vector.ByteCapacity <= sizeof(short));

        if (vector.Data.Length == 0)
        {
            return default;
        }
        return (short)(vector.Data[0] & NC.Short);
    }

    public static explicit operator ushort(BitVector vector)
    {
        Debug.Assert(vector.ByteCapacity <= sizeof(ushort));

        if (vector.Data.Length == 0)
        {
            return default;
        }
        return (ushort)(vector.Data[0] & NC.Short);
    }

    public static explicit operator int(BitVector vector)
    {
        Debug.Assert(vector.ByteCapacity <= sizeof(int));

        if (vector.Data.Length == 0)
        {
            return default;
        }
        return vector.Data[0];
    }

    public static explicit operator uint(BitVector vector)
    {
        Debug.Assert(vector.ByteCapacity <= sizeof(uint));

        if (vector.Data.Length == 0)
        {
            return default;
        }
        return (uint)vector.Data[0];
    }

    public static explicit operator long(BitVector vector)
    {
        Debug.Assert(vector.ByteCapacity <= sizeof(long));

        if (vector.Data.Length == 0)
        {
            return default;
        }
        if (vector.Data.Length == 1)
        {
            return (uint)vector.Data[0];
        }
        return ((long)vector.Data[1] << (sizeof(int) * NC.BitsPerByte)) | (uint)vector.Data[0];
    }

    public static explicit operator ulong(BitVector vector)
    {
        Debug.Assert(vector.ByteCapacity <= sizeof(ulong));

        if (vector.Data.Length == 0)
        {
            return default;
        }
        if (vector.Data.Length == 1)
        {
            return (uint)vector.Data[0];
        }
        return ((ulong)vector.Data[1] << (sizeof(uint) * NC.BitsPerByte)) | (uint)vector.Data[0];
    }

    public static implicit operator string(BitVector vector)
    {
        return vector.ToString();
    }

    #endregion Explicit operators
}
