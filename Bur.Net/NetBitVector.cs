using Bur.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Bur.Net
{
    /// <summary>
    /// Fixed size vector of booleans.
    /// </summary>
    /// <remarks>
    /// Source: https://github.com/lidgren/lidgren-network-gen3
    /// </remarks>
    public sealed class NetBitVector : IEnumerable<bool>, IEquatable<NetBitVector>
    {
        private const int DataElementSize = 8 * sizeof(int);

        private readonly int capacity;
        private readonly int[] data;
        private int setBitCount;

        /// <summary>
        /// Creates bit vector.
        /// </summary>
        /// <param name="capacity">Number of bits.</param>
        public NetBitVector(int capacity)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }

            this.capacity = capacity;
            this.data = new int[(capacity + (DataElementSize - 1)) / DataElementSize];

            var byteSize = 8 * sizeof(byte);
            ByteCapacity = (this.capacity + (byteSize - 1)) / byteSize;
        }

        /// <summary>
        /// Creates bit vector from bytes.
        /// </summary>
        /// <param name="bytes">Source bytes.</param>
        public NetBitVector(byte[] bytes) : this(8 * bytes.Length)
        {
            var b = 0;
            for (int i = 0; i < data.Length; i++)
            {
                for (int j = 0; j < sizeof(int); j++)
                {
                    data[i] |= bytes[b++] << (j * 8);
                    if (b * 8 >= capacity)
                    {
                        goto End;
                    }
                }
            }
            End: return;
        }

        /// <summary>
        /// Gets the number of bits/booleans stored in this vector.
        /// </summary>
        public int Capacity => capacity;

        /// <summary>
        /// Gets the number of bytes to store all bits.
        /// </summary>
        public int ByteCapacity { get; }

        /// <summary>
        /// Returns true if all bits/booleans are set to zero/false.
        /// </summary>
        public bool IsEmpty => setBitCount == 0;

        /// <summary>
        /// Returns the number of bits/booleans set to one/true.
        /// </summary>
        public int Count => setBitCount;

        /// <summary>
        /// Gets the bit/bool at the specified index.
        /// </summary>
        [IndexerName("Bit")]
        public bool this[int bitIndex]
        {
            get { return Get(bitIndex); }
            set { Set(bitIndex, value); }
        }

        /// <summary>
        /// Shift all bits one step down, cycling the first bit to the top.
        /// </summary>
        public void RotateDown()
        {
            var lengthMinusOne = data.Length - 1;

            var firstBit = data[0] & 1;
            for (int i = 0; i < lengthMinusOne; i++)
            {
                data[i] = ((data[i] >> 1) & ~(1 << (DataElementSize - 1))) | data[i + 1] << (DataElementSize - 1);
            }

            var lastIndex = capacity - 1 - (DataElementSize * lengthMinusOne);

            // Special handling of last int
            var cur = data[lengthMinusOne];
            cur >>= 1;
            cur |= firstBit << lastIndex;

            data[lengthMinusOne] = cur;
        }

        /// <summary>
        /// Gets the first (lowest) index set to true.
        /// </summary>
        public int GetFirstSetIndex()
        {
            var byteIndex = 0;

            var @byte = data[0];
            while (@byte == 0)
            {
                byteIndex++;
                @byte = data[byteIndex];
            }

            var bitIndex = 0;
            while (((@byte >> bitIndex) & 1) == 0)
            {
                bitIndex++;
            }

            return (byteIndex * DataElementSize) + bitIndex;
        }

        /// <summary>
        /// Gets the bit/bool at the specified index.
        /// </summary>
        public bool Get(int bitIndex)
        {
            NetException.Assert(bitIndex >= 0 && bitIndex < capacity);

            return (data[bitIndex / DataElementSize] & (1 << (bitIndex % DataElementSize))) != 0;
        }

        /// <summary>
        /// Sets or clears the bit/bool at the specified index.
        /// </summary>
        public void Set(int bitIndex, bool value)
        {
            NetException.Assert(bitIndex >= 0 && bitIndex < capacity);

            var byteIndex = bitIndex / DataElementSize;
            if (value)
            {
                if ((data[byteIndex] & (1 << (bitIndex % DataElementSize))) == 0)
                {
                    setBitCount++;
                }
                data[byteIndex] |= (1 << (bitIndex % DataElementSize));
            }
            else
            {
                if ((data[byteIndex] & (1 << (bitIndex % DataElementSize))) != 0)
                {
                    setBitCount--;
                }
                data[byteIndex] &= (~(1 << (bitIndex % DataElementSize)));
            }
        }

        /// <summary>
        /// Sets the bit/bool at the specified index.
        /// </summary>
        public void Set(int bitIndex)
        {
            Set(bitIndex, true);
        }

        /// <summary>
        /// Clears the bit/bool at the specified index.
        /// </summary>
        public void Clear(int bitIndex)
        {
            Set(bitIndex, false);
        }

        /// <summary>
        /// Sets bits/bools at the specified indexes.
        /// </summary>
        public void Set(params int[] bitIndexes)
        {
            foreach (var bitIndex in bitIndexes)
            {
                Set(bitIndex, true);
            }
        }

        /// <summary>
        /// Clears bits/bools at the specified indexes.
        /// </summary>
        public void Clear(params int[] bitIndexes)
        {
            foreach (var bitIndex in bitIndexes)
            {
                Set(bitIndex, false);
            }
        }

        /// <summary>
        /// Sets all bits/booleans to one/true.
        /// </summary>
        public void SetAll()
        {
            Array.Fill(data, int.MaxValue);
            setBitCount = capacity;
        }

        /// <summary>
        /// Sets all bits/booleans to zero/false.
        /// </summary>
        public void ClearAll()
        {
            Array.Clear(data, 0, data.Length);
            setBitCount = 0;
        }

        /// <summary>
        /// Returns a string that represents this object.
        /// </summary>
        public override string ToString()
        {
            var sb = new StringBuilder(capacity + 2);
            sb.Append('[');
            for (int i = capacity - 1; i >= 0; i--)
            {
                sb.Append(Get(i) ? '1' : '0');
            }
            sb.Append(']');
            return sb.ToString();
        }

        /// <summary>
        /// Returns a byte array.
        /// </summary>
        public byte[] ToBytes()
        {
            var bytes = new byte[ByteCapacity];
            var b = 0;
            for (int i = 0; i < data.Length; i++)
            {
                for (int j = 0; j < sizeof(int); j++)
                {
                    bytes[b++] = (byte)((data[i] >> (j * 8)) & 0xFF);
                    if (b * 8 >= capacity)
                    {
                        goto End;
                    }
                }
            }
            End: return bytes;
        }

        #region IEnumerable

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator<bool> IEnumerable<bool>.GetEnumerator()
        {
            return GetEnumerator();
        }

        private IEnumerator<bool> GetEnumerator()
        {
            for (int i = 0; i < capacity; i++)
            {
                yield return Get(i);
            }
        }

        #endregion IEnumerable

        #region IEquatable

        private static readonly ArrayEqualityComparer<int> dataComparer = new ArrayEqualityComparer<int>();

        public static bool operator ==(NetBitVector left, NetBitVector right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }
            if (ReferenceEquals(left, null))
            {
                return false;
            }
            if (ReferenceEquals(right, null))
            {
                return false;
            }
            return left.EqualsCore(right);
        }

        public static bool operator !=(NetBitVector left, NetBitVector right)
        {
            return !(left == right);
        }

        public bool Equals(NetBitVector other)
        {
            if (ReferenceEquals(other, this))
            {
                return true;
            }
            if (ReferenceEquals(other, null))
            {
                return false;
            }
            return EqualsCore(other);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(obj, this))
            {
                return true;
            }
            if (ReferenceEquals(obj, null))
            {
                return false;
            }
            return obj is NetBitVector other && EqualsCore(other);
        }

        public override int GetHashCode()
        {
            return dataComparer.GetHashCode(data);
        }

        private bool EqualsCore(NetBitVector other)
        {
            if (capacity != other.capacity)
            {
                return false;
            }
            return dataComparer.Equals(data, other.data);
        }

        #endregion IEquatable

        #region Explicit operators

        public static explicit operator sbyte(NetBitVector vector)
        {
            NetException.Assert(vector.ByteCapacity <= sizeof(sbyte));

            if (vector.data.Length == 0)
            {
                return default;
            }
            return (sbyte)(vector.data[0] & 0xFF);
        }

        public static explicit operator byte(NetBitVector vector)
        {
            NetException.Assert(vector.ByteCapacity <= sizeof(byte));

            if (vector.data.Length == 0)
            {
                return default;
            }
            return (byte)(vector.data[0] & 0xFF);
        }

        public static explicit operator short(NetBitVector vector)
        {
            NetException.Assert(vector.ByteCapacity <= sizeof(short));

            if (vector.data.Length == 0)
            {
                return default;
            }
            return (short)(vector.data[0] & 0xFFFF);
        }

        public static explicit operator ushort(NetBitVector vector)
        {
            NetException.Assert(vector.ByteCapacity <= sizeof(ushort));

            if (vector.data.Length == 0)
            {
                return default;
            }
            return (ushort)(vector.data[0] & 0xFFFF);
        }

        public static explicit operator int(NetBitVector vector)
        {
            NetException.Assert(vector.ByteCapacity <= sizeof(int));

            if (vector.data.Length == 0)
            {
                return default;
            }
            return (int)vector.data[0];
        }

        public static explicit operator uint(NetBitVector vector)
        {
            NetException.Assert(vector.ByteCapacity <= sizeof(uint));

            if (vector.data.Length == 0)
            {
                return default;
            }
            return (uint)vector.data[0];
        }

        public static explicit operator long(NetBitVector vector)
        {
            NetException.Assert(vector.ByteCapacity <= sizeof(long));

            if (vector.data.Length == 0)
            {
                return default;
            }
            if (vector.data.Length == 1)
            {
                return (uint)vector.data[0];
            }
            return ((long)vector.data[1] << 32) | (uint)vector.data[0];
        }

        public static explicit operator ulong(NetBitVector vector)
        {
            NetException.Assert(vector.ByteCapacity <= sizeof(ulong));

            if (vector.data.Length == 0)
            {
                return default;
            }
            if (vector.data.Length == 1)
            {
                return (uint)vector.data[0];
            }
            return ((ulong)vector.data[1] << 32) | (uint)vector.data[0];
        }

        public static explicit operator string(NetBitVector vector)
        {
            return vector.ToString();
        }

        #endregion Explicit operators
    }
}
