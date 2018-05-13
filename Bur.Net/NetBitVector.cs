using System;
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
    public sealed class NetBitVector
    {
        private const int byteSize = 8 * sizeof(int);

        private readonly int capacity;
        private readonly int[] data;
        private int setBitCount;

        /// <summary>
        /// Gets the number of bits/booleans stored in this vector.
        /// </summary>
        public int Capacity => capacity;

        /// <summary>
        /// NetBitVector constructor.
        /// </summary>
        public NetBitVector(int bitsCapacity)
        {
            capacity = bitsCapacity;
            data = new int[(bitsCapacity + (byteSize - 1)) / byteSize];
        }

        /// <summary>
        /// Returns true if all bits/booleans are set to zero/false.
        /// </summary>
        public bool IsEmpty()
        {
            return setBitCount == 0;
        }

        /// <summary>
        /// Returns the number of bits/booleans set to one/true.
        /// </summary>
        public int Count()
        {
            return setBitCount;
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
                data[i] = ((data[i] >> 1) & ~(1 << (byteSize - 1))) | data[i + 1] << (byteSize - 1);
            }

            var lastIndex = capacity - 1 - (byteSize * lengthMinusOne);

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

            return (byteIndex * byteSize) + bitIndex;
        }

        /// <summary>
        /// Gets the bit/bool at the specified index.
        /// </summary>
        public bool Get(int bitIndex)
        {
            NetException.Assert(bitIndex >= 0 && bitIndex < capacity);

            return (data[bitIndex / byteSize] & (1 << (bitIndex % byteSize))) != 0;
        }

        /// <summary>
        /// Sets or clears the bit/bool at the specified index.
        /// </summary>
        public void Set(int bitIndex, bool value)
        {
            NetException.Assert(bitIndex >= 0 && bitIndex < capacity);

            var byteIndex = bitIndex / byteSize;
            if (value)
            {
                if ((data[byteIndex] & (1 << (bitIndex % byteSize))) == 0)
                {
                    setBitCount++;
                }
                data[byteIndex] |= (1 << (bitIndex % byteSize));
            }
            else
            {
                if ((data[byteIndex] & (1 << (bitIndex % byteSize))) != 0)
                {
                    setBitCount--;
                }
                data[byteIndex] &= (~(1 << (bitIndex % byteSize)));
            }
        }

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
    }
}
