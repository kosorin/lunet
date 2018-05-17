using Bur.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Bur.Net
{
    /// <summary>
    /// Fixed size vector of booleans.
    /// </summary>
    public sealed class NetBitVector : IEnumerable<bool>, IEquatable<NetBitVector>
    {
        private const int BitsPerInt32 = 8 * sizeof(int);
        private const int BitsPerByte = 8;
        private const int BytesPerInt32 = 4;

        private readonly int[] data;
        private readonly int capacity;

        /// <summary>
        /// Creates a bit vector with capacity for one byte.
        /// </summary>
        public NetBitVector()
            : this(BitsPerByte, false)
        {
        }

        /// <summary>
        /// Creates a bit vector.
        /// </summary>
        /// <param name="capacity">Number of bits.</param>
        public NetBitVector(int capacity)
            : this(capacity, false)
        {
        }

        /// <summary>
        /// Creates a bit vector with default value.
        /// </summary>
        /// <param name="capacity">Number of bits.</param>
        /// <param name="defaultValue">Value to assign to each bit.</param>
        public NetBitVector(int capacity, bool defaultValue)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }

            this.capacity = capacity;
            ByteCapacity = GetCapacity(capacity, BitsPerByte);

            data = new int[GetCapacity(capacity, BitsPerInt32)];

            SetAll(defaultValue);
        }

        /// <summary>
        /// Creates a bit vector from bytes.
        /// </summary>
        /// <param name="bytes">Source bytes.</param>
        public NetBitVector(byte[] bytes)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            capacity = bytes.Length * BitsPerByte;
            ByteCapacity = GetCapacity(capacity, BitsPerByte);

            data = new int[GetCapacity(capacity, BitsPerInt32)];

            for (int i = 0, b = 0; i < data.Length; i++)
            {
                for (int j = 0; j < BytesPerInt32; j++)
                {
                    data[i] |= bytes[b++] << (j * BitsPerByte);
                    if (b >= capacity / BitsPerByte)
                    {
                        goto End;
                    }
                }
            }
            End: return;
        }

        /// <summary>
        /// Creates a bit vector from another bit vector.
        /// </summary>
        /// <param name="source">Source bit vector.</param>
        public NetBitVector(NetBitVector source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            capacity = source.capacity;
            ByteCapacity = GetCapacity(capacity, BitsPerByte);

            data = new int[source.data.Length];

            Array.Copy(source.data, 0, data, 0, data.Length);
        }

        /// <summary>
        /// Gets the number of bits stored in this vector.
        /// </summary>
        public int Capacity => capacity;

        /// <summary>
        /// Gets the number of bytes to store all bits.
        /// </summary>
        public int ByteCapacity { get; }

        /// <summary>
        /// Gets the bit at the specified index.
        /// </summary>
        [IndexerName("Bit")]
        public bool this[int bitIndex]
        {
            get { return Get(bitIndex); }
            set { Set(bitIndex, value); }
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

            var lengthToClear = data.Length;
            if (count < capacity)
            {
                lengthToClear = count / BitsPerInt32;

                var lastIndex = (capacity - 1) / BitsPerInt32;
                var shiftCount = count - (lengthToClear * BitsPerInt32);

                if (shiftCount == 0)
                {
                    Array.Copy(data, 0, data, lengthToClear, lastIndex + 1 - lengthToClear);
                }
                else
                {
                    var fromIndex = lastIndex - lengthToClear;
                    unchecked
                    {
                        while (fromIndex > 0)
                        {
                            int left = data[fromIndex] << shiftCount;
                            uint right = (uint)data[--fromIndex] >> (BitsPerInt32 - shiftCount);
                            data[lastIndex] = left | (int)right;
                            lastIndex--;
                        }
                        data[lastIndex] = data[fromIndex] << shiftCount;
                    }
                }
            }

            if (lengthToClear > 0)
            {
                Array.Clear(data, 0, lengthToClear);
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
            var length = data.Length;
            if (count < capacity)
            {
                var fromIndex = count / BitsPerInt32;
                var shiftCount = count - (fromIndex * BitsPerInt32);

                if (shiftCount == 0)
                {
                    unchecked
                    {
                        uint mask = uint.MaxValue >> (BitsPerInt32 - (capacity % BitsPerInt32));
                        data[length - 1] &= (int)mask;
                    }
                    Array.Copy(data, fromIndex, data, 0, length - fromIndex);
                    clearToIndex = length - fromIndex;
                }
                else
                {
                    var lastIndex = length - 1;
                    unchecked
                    {
                        while (fromIndex < lastIndex)
                        {
                            uint right = (uint)data[fromIndex] >> shiftCount;
                            int left = data[++fromIndex] << (BitsPerInt32 - shiftCount);
                            data[clearToIndex++] = left | (int)right;
                        }
                        uint mask = uint.MaxValue >> (BitsPerInt32 - (capacity % BitsPerInt32));
                        mask &= (uint)data[fromIndex];
                        data[clearToIndex++] = (int)(mask >> shiftCount);
                    }
                }
            }

            var lengthToClear = length - clearToIndex;
            if (lengthToClear > 0)
            {
                Array.Clear(data, clearToIndex, lengthToClear);
            }
        }

        /// <summary>
        /// Rotate all bits to right.
        /// </summary>
        public void RightRotate()
        {
            var lengthMinusOne = data.Length - 1;

            int firstBit = data[0] & 1;
            for (int i = 0; i < lengthMinusOne; i++)
            {
                data[i] = ((data[i] >> 1) & ~(1 << (BitsPerInt32 - 1))) | data[i + 1] << (BitsPerInt32 - 1);
            }

            var lastIndex = capacity - 1 - (BitsPerInt32 * lengthMinusOne);

            int last = data[lengthMinusOne];
            last >>= 1;
            last |= firstBit << lastIndex;

            data[lengthMinusOne] = last;
        }

        /// <summary>
        /// Gets the first (lowest) index set to true.
        /// </summary>
        public int GetFirstSetBitIndex()
        {
            var byteIndex = 0;

            int @byte = data[0];
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

            return (byteIndex * BitsPerInt32) + bitIndex;
        }

        /// <summary>
        /// Gets the number of set bits in vector.
        /// </summary>
        public int GetNumberOfSetBits()
        {
            var count = 0;
            for (int i = 0; i < data.Length; i++)
            {
                int x = data[i];
#pragma warning disable RCS1058 // Use compound assignment.
                x = x - ((x >> 1) & 0x55555555);
                x = (x & 0x33333333) + ((x >> 2) & 0x33333333);
                x = (((x + (x >> 4)) & 0x0F0F0F0F) * 0x01010101) >> 24;
#pragma warning restore RCS1058 // Use compound assignment.
                count += x;
            }
            return count;
        }

        /// <summary>
        /// Gets the number of clear bits in vector.
        /// </summary>
        public int GetNumberOfClearBits()
        {
            return capacity - GetNumberOfSetBits();
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
            return GetNumberOfSetBits() == capacity;
        }

        /// <summary>
        /// Gets the bit at the specified index.
        /// </summary>
        public bool Get(int index)
        {
            if (index < 0 || index > capacity)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return (data[index / BitsPerInt32] & (1 << (index % BitsPerInt32))) != 0;
        }

        /// <summary>
        /// Sets or clears the bit at the specified index.
        /// </summary>
        public void Set(int index, bool value)
        {
            if (index < 0 || index > capacity)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            if (value)
            {
                data[index / BitsPerInt32] |= (1 << (index % BitsPerInt32));
            }
            else
            {
                data[index / BitsPerInt32] &= ~(1 << (index % BitsPerInt32));
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
        /// Clears the bit at the specified index.
        /// </summary>
        public void Clear(int index)
        {
            Set(index, false);
        }

        /// <summary>
        /// Sets bits at the specified indexes.
        /// </summary>
        public void SetAll(params int[] indexes)
        {
            foreach (var index in indexes)
            {
                Set(index, true);
            }
        }

        /// <summary>
        /// Clears bits at the specified indexes.
        /// </summary>
        public void ClearAll(params int[] indexes)
        {
            foreach (var index in indexes)
            {
                Set(index, false);
            }
        }

        /// <summary>
        /// Sets all bits to the value.
        /// </summary>
        public void SetAll(bool value)
        {
            int fillValue = value ? unchecked((int)0xFFFFFFFF) : 0;
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = fillValue;
            }
        }

        /// <summary>
        /// Sets all bits.
        /// </summary>
        public void SetAll()
        {
            SetAll(true);
        }

        /// <summary>
        /// Clears all bits.
        /// </summary>
        public void ClearAll()
        {
            SetAll(false);
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
            for (int i = 0, b = 0; i < data.Length; i++)
            {
                for (int j = 0; j < BytesPerInt32; j++)
                {
                    bytes[b++] = (byte)((data[i] >> (j * BitsPerByte)) & 0xFF);
                    if (b >= capacity / BitsPerByte)
                    {
                        goto End;
                    }
                }
            }
            End: return bytes;
        }

        private static int GetCapacity(int n, int div)
        {
            Debug.Assert(div > 0, $"{nameof(GetCapacity)}: {nameof(div)} argument must be greater than 0");

            return n > 0
                ? ((n - 1) / div) + 1
                : 0;
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

        public static bool operator !=(NetBitVector left, NetBitVector right)
        {
            return !(left == right);
        }

        public bool Equals(NetBitVector other)
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

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            if (obj is null)
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
            Debug.Assert(vector.ByteCapacity <= sizeof(sbyte));

            if (vector.data.Length == 0)
            {
                return default;
            }
            return (sbyte)(vector.data[0] & 0xFF);
        }

        public static explicit operator byte(NetBitVector vector)
        {
            Debug.Assert(vector.ByteCapacity <= sizeof(byte));

            if (vector.data.Length == 0)
            {
                return default;
            }
            return (byte)(vector.data[0] & 0xFF);
        }

        public static explicit operator short(NetBitVector vector)
        {
            Debug.Assert(vector.ByteCapacity <= sizeof(short));

            if (vector.data.Length == 0)
            {
                return default;
            }
            return (short)(vector.data[0] & 0xFFFF);
        }

        public static explicit operator ushort(NetBitVector vector)
        {
            Debug.Assert(vector.ByteCapacity <= sizeof(ushort));

            if (vector.data.Length == 0)
            {
                return default;
            }
            return (ushort)(vector.data[0] & 0xFFFF);
        }

        public static explicit operator int(NetBitVector vector)
        {
            Debug.Assert(vector.ByteCapacity <= sizeof(int));

            if (vector.data.Length == 0)
            {
                return default;
            }
            return (int)vector.data[0];
        }

        public static explicit operator uint(NetBitVector vector)
        {
            Debug.Assert(vector.ByteCapacity <= sizeof(uint));

            if (vector.data.Length == 0)
            {
                return default;
            }
            return (uint)vector.data[0];
        }

        public static explicit operator long(NetBitVector vector)
        {
            Debug.Assert(vector.ByteCapacity <= sizeof(long));

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
            Debug.Assert(vector.ByteCapacity <= sizeof(ulong));

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
