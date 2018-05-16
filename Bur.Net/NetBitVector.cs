using Bur.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using DataElement = System.Int32;

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
        private const int byteSize = 8 * sizeof(byte);
        private const int dataElementSize = 8 * sizeof(DataElement);

        private static readonly ArrayEqualityComparer<int> dataComparer = new ArrayEqualityComparer<DataElement>();

        private readonly int capacity;
        private readonly DataElement[] data;
        private int setBitCount;

        /// <summary>
        /// NetBitVector constructor.
        /// </summary>
        /// <param name="capacity">Number of bits.</param>
        public NetBitVector(int capacity)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }

            this.capacity = capacity;
            this.data = new DataElement[(capacity + (dataElementSize - 1)) / dataElementSize];

            ByteCapacity = (this.capacity + (byteSize - 1)) / byteSize;
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

        /// <summary>
        /// Shift all bits one step down, cycling the first bit to the top.
        /// </summary>
        public void RotateDown()
        {
            var lengthMinusOne = data.Length - 1;

            var firstBit = data[0] & 1;
            for (int i = 0; i < lengthMinusOne; i++)
            {
                data[i] = ((data[i] >> 1) & ~(1 << (dataElementSize - 1))) | data[i + 1] << (dataElementSize - 1);
            }

            var lastIndex = capacity - 1 - (dataElementSize * lengthMinusOne);

            // Special handling of last int
            var cur = data[lengthMinusOne];
            cur = cur >> 1;
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

            return (byteIndex * dataElementSize) + bitIndex;
        }

        /// <summary>
        /// Gets the bit/bool at the specified index.
        /// </summary>
        public bool Get(int bitIndex)
        {
            NetException.Assert(bitIndex >= 0 && bitIndex < capacity);

            return (data[bitIndex / dataElementSize] & (1 << (bitIndex % dataElementSize))) != 0;
        }

        /// <summary>
        /// Sets or clears the bit/bool at the specified index.
        /// </summary>
        public void Set(int bitIndex, bool value)
        {
            NetException.Assert(bitIndex >= 0 && bitIndex < capacity);

            var byteIndex = bitIndex / dataElementSize;
            if (value)
            {
                if ((data[byteIndex] & (1 << (bitIndex % dataElementSize))) == 0)
                {
                    setBitCount++;
                }
                data[byteIndex] |= (1 << (bitIndex % dataElementSize));
            }
            else
            {
                if ((data[byteIndex] & (1 << (bitIndex % dataElementSize))) != 0)
                {
                    setBitCount--;
                }
                data[byteIndex] &= (~(1 << (bitIndex % dataElementSize)));
            }
        }

        /// <summary>
        /// Sets all bits/booleans to zero/false.
        /// </summary>
        public void Clear()
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
                for (int j = 0; j < sizeof(DataElement); j++)
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

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator<bool> IEnumerable<bool>.GetEnumerator()
        {
            return GetEnumerator();
        }

        private bool EqualsCore(NetBitVector other)
        {
            if (capacity != other.capacity)
            {
                return false;
            }
            return dataComparer.Equals(data, other.data);
        }

        private IEnumerator<bool> GetEnumerator()
        {
            for (int i = 0; i < capacity; i++)
            {
                yield return Get(i);
            }
        }
    }
}
