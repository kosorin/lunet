using Lunet.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace Lunet.Data
{
    /// <summary>
    /// Fixed size vector of booleans.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0007:Use implicit type", Justification = "<Pending>")]
    public sealed class BitVector : IEquatable<BitVector>
    {
        public static readonly BitVector Empty = new BitVector(0);

        private readonly int[] _data;
        private readonly int _capacity;

        /// <summary>
        /// Initializes a new instance of the <see cref="BitVector"/> class with capacity for one byte.
        /// </summary>
        public BitVector()
            : this(NC.BitsPerByte, false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BitVector"/> class.
        /// </summary>
        /// <param name="capacity">Number of bits.</param>
        public BitVector(int capacity)
            : this(capacity, false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BitVector"/> class with default bit values.
        /// </summary>
        /// <param name="capacity">Number of bits.</param>
        /// <param name="defaultValue">Value to assign to each bit.</param>
        public BitVector(int capacity, bool defaultValue)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }

            _capacity = capacity;
            ByteCapacity = NetHelper.GetElementCapacity(capacity, NC.BitsPerByte);

            _data = new int[NetHelper.GetElementCapacity(capacity, NC.BitsPerInt)];

            SetAll(defaultValue);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BitVector"/> class with bytes.
        /// </summary>
        /// <param name="bytes">Source bytes.</param>
        public BitVector(byte[] bytes)
            : this(bytes, bytes.Length * NC.BitsPerByte)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BitVector"/> class with bytes.
        /// </summary>
        /// <param name="bytes">Source bytes.</param>
        /// <param name="capacity">Number of bits.</param>
        public BitVector(byte[] bytes, int capacity)
            : this(new ReadOnlySpan<byte>(bytes), capacity)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BitVector"/> class with bytes.
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

            _capacity = capacity;
            ByteCapacity = NetHelper.GetElementCapacity(capacity, NC.BitsPerByte);

            _data = new int[NetHelper.GetElementCapacity(capacity, NC.BitsPerInt)];

            for (int i = 0, b = 0; i < _data.Length; i++)
            {
                for (int j = 0; j < sizeof(int); j++)
                {
                    _data[i] |= bytes[b] << (j * NC.BitsPerByte);
                    if (++b >= ByteCapacity)
                    {
                        goto End;
                    }
                }
            }
        End:
            return;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BitVector"/> class with another bit vector.
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
            if (count < 0 || count > source._capacity)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }
            if (offset < 0 || offset + count > source._capacity)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            _capacity = count;
            ByteCapacity = NetHelper.GetElementCapacity(_capacity, NC.BitsPerByte);

            _data = new int[NetHelper.GetElementCapacity(_capacity, NC.BitsPerInt)];

            Array.Copy(source._data, 0, _data, 0, _data.Length);
        }


        /// <summary>
        /// Gets the number of bits stored in this vector.
        /// </summary>
        public int Capacity => _capacity;

        /// <summary>
        /// Gets the number of bytes to store all bits.
        /// </summary>
        public int ByteCapacity { get; }

        internal int[] Data => _data;


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

            var lengthToClear = _data.Length;
            if (count < _capacity)
            {
                lengthToClear = count / NC.BitsPerInt;

                var lastIndex = (_capacity - 1) / NC.BitsPerInt;
                var shiftCount = count - (lengthToClear * NC.BitsPerInt);

                if (shiftCount == 0)
                {
                    Array.Copy(_data, 0, _data, lengthToClear, lastIndex + 1 - lengthToClear);
                }
                else
                {
                    var fromIndex = lastIndex - lengthToClear;
                    unchecked
                    {
                        while (fromIndex > 0)
                        {
                            int left = _data[fromIndex] << shiftCount;
                            uint right = (uint)_data[--fromIndex] >> (NC.BitsPerInt - shiftCount);
                            _data[lastIndex] = left | (int)right;
                            lastIndex--;
                        }
                        _data[lastIndex] = _data[fromIndex] << shiftCount;
                    }
                }
            }

            if (lengthToClear > 0)
            {
                Array.Clear(_data, 0, lengthToClear);
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
            var length = _data.Length;
            if (count < _capacity)
            {
                var fromIndex = count / NC.BitsPerInt;
                var shiftCount = count - (fromIndex * NC.BitsPerInt);

                if (shiftCount == 0)
                {
                    unchecked
                    {
                        uint mask = uint.MaxValue >> (NC.BitsPerInt - (_capacity % NC.BitsPerInt));
                        _data[length - 1] &= (int)mask;
                    }
                    Array.Copy(_data, fromIndex, _data, 0, length - fromIndex);
                    clearToIndex = length - fromIndex;
                }
                else
                {
                    var lastIndex = length - 1;
                    unchecked
                    {
                        while (fromIndex < lastIndex)
                        {
                            uint right = (uint)_data[fromIndex] >> shiftCount;
                            int left = _data[++fromIndex] << (NC.BitsPerInt - shiftCount);
                            _data[clearToIndex++] = left | (int)right;
                        }
                        uint mask = uint.MaxValue >> (NC.BitsPerInt - (_capacity % NC.BitsPerInt));
                        mask &= (uint)_data[fromIndex];
                        _data[clearToIndex++] = (int)(mask >> shiftCount);
                    }
                }
            }

            var lengthToClear = length - clearToIndex;
            if (lengthToClear > 0)
            {
                Array.Clear(_data, clearToIndex, lengthToClear);
            }
        }


        /// <summary>
        /// Rotate all bits to right.
        /// </summary>
        public void RightRotate()
        {
            var lengthMinusOne = _data.Length - 1;

            int firstBit = _data[0] & NC.One;
            for (int i = 0; i < lengthMinusOne; i++)
            {
                _data[i] = ((_data[i] >> 1) & ~(1 << (NC.BitsPerInt - 1))) | _data[i + 1] << (NC.BitsPerInt - 1);
            }

            var lastIndex = _capacity - 1 - (NC.BitsPerInt * lengthMinusOne);

            int last = _data[lengthMinusOne];
            last >>= 1;
            last |= firstBit << lastIndex;

            _data[lengthMinusOne] = last;
        }


        /// <summary>
        /// Gets the first (lowest) index set to true.
        /// </summary>
        public int GetFirstSetBitIndex()
        {
            var byteIndex = 0;

            int @byte = _data[0];
            while (@byte == 0)
            {
                byteIndex++;
                @byte = _data[byteIndex];
            }

            var bitIndex = 0;
            while (((@byte >> bitIndex) & NC.One) == 0)
            {
                bitIndex++;
            }

            return (byteIndex * NC.BitsPerInt) + bitIndex;
        }

        /// <summary>
        /// Gets the number of set bits in vector.
        /// </summary>
        public int GetNumberOfSetBits()
        {
            var count = 0;
            for (int i = 0; i < _data.Length; i++)
            {
                int x = _data[i];
                x -= ((x >> 1) & 0x55555555);
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
            return _capacity - GetNumberOfSetBits();
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
            return GetNumberOfSetBits() == _capacity;
        }


        /// <summary>
        /// Gets the bit at the specified index.
        /// </summary>
        public bool Get(int index)
        {
            if (index < 0 || index >= _capacity)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return (_data[index / NC.BitsPerInt] & (NC.One << (index % NC.BitsPerInt))) != 0;
        }

        /// <summary>
        /// Sets or clears the bit at the specified index.
        /// </summary>
        public void Set(int index, bool value)
        {
            if (index < 0 || index >= _capacity)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            if (value)
            {
                _data[index / NC.BitsPerInt] |= (NC.One << (index % NC.BitsPerInt));
            }
            else
            {
                _data[index / NC.BitsPerInt] &= ~(NC.One << (index % NC.BitsPerInt));
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
            int fillValue = value ? unchecked((int)NC.Int) : NC.Zero;
            for (int i = 0; i < _data.Length; i++)
            {
                _data[i] = fillValue;
            }
        }

        /// <summary>
        /// Sets bits at the specified indexes.
        /// </summary>
        public void SetAll(params int[] indexes)
        {
            for (int i = 0; i < indexes.Length; i++)
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
            for (int i = 0; i < indexes.Length; i++)
            {
                Set(indexes[i], false);
            }
        }


        /// <summary>
        /// Returns a string that represents this object.
        /// </summary>
        public override string ToString()
        {
            var sb = new StringBuilder(_capacity + 2);
            sb.Append('[');

            const int lastOne = NC.One << (NC.BitsPerInt - 1);
            for (int i = _data.Length - 1, b = _capacity; i >= 0; i--)
            {
                var startBitIndex = (NC.BitsPerInt - (b % NC.BitsPerInt)) % NC.BitsPerInt;
                var data = _data[i] << startBitIndex;
                for (int j = startBitIndex; j < NC.BitsPerInt; j++)
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
            for (int i = 0, b = 0; i < _data.Length; i++)
            {
                for (int j = 0; j < sizeof(int); j++)
                {
                    var @byte = (byte)((_data[i] >> (j * NC.BitsPerByte)) & NC.Byte);
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
            for (int i = 0, b = 0; i < _data.Length; i++)
            {
                var data = _data[i];
                for (int j = 0; j < sizeof(int) * NC.BitsPerByte; j++)
                {
                    var bit = (data & NC.One) == NC.One;
                    yield return bit;
                    if (++b >= _capacity)
                    {
                        yield break;
                    }
                    data >>= 1;
                }
            }
        }

        public BitVector Clone()
        {
            return new BitVector(this, 0, _capacity);
        }

        public BitVector Clone(int offset, int count)
        {
            return new BitVector(this, offset, count);
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

        public bool Equals(BitVector other)
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
            return obj is BitVector other && EqualsCore(other);
        }

        public override int GetHashCode()
        {
            return DataComparer.GetHashCode(_data);
        }

        private bool EqualsCore(BitVector other)
        {
            if (_capacity != other._capacity)
            {
                return false;
            }
            return DataComparer.Equals(_data, other._data);
        }

        #endregion IEquatable

        #region Explicit operators

        public static explicit operator sbyte(BitVector vector)
        {
            Debug.Assert(vector.ByteCapacity <= sizeof(sbyte));

            if (vector._data.Length == 0)
            {
                return default;
            }
            return (sbyte)(vector._data[0] & NC.Byte);
        }

        public static explicit operator byte(BitVector vector)
        {
            Debug.Assert(vector.ByteCapacity <= sizeof(byte));

            if (vector._data.Length == 0)
            {
                return default;
            }
            return (byte)(vector._data[0] & NC.Byte);
        }

        public static explicit operator short(BitVector vector)
        {
            Debug.Assert(vector.ByteCapacity <= sizeof(short));

            if (vector._data.Length == 0)
            {
                return default;
            }
            return (short)(vector._data[0] & NC.Short);
        }

        public static explicit operator ushort(BitVector vector)
        {
            Debug.Assert(vector.ByteCapacity <= sizeof(ushort));

            if (vector._data.Length == 0)
            {
                return default;
            }
            return (ushort)(vector._data[0] & NC.Short);
        }

        public static explicit operator int(BitVector vector)
        {
            Debug.Assert(vector.ByteCapacity <= sizeof(int));

            if (vector._data.Length == 0)
            {
                return default;
            }
            return (int)vector._data[0];
        }

        public static explicit operator uint(BitVector vector)
        {
            Debug.Assert(vector.ByteCapacity <= sizeof(uint));

            if (vector._data.Length == 0)
            {
                return default;
            }
            return (uint)vector._data[0];
        }

        public static explicit operator long(BitVector vector)
        {
            Debug.Assert(vector.ByteCapacity <= sizeof(long));

            if (vector._data.Length == 0)
            {
                return default;
            }
            if (vector._data.Length == 1)
            {
                return (uint)vector._data[0];
            }
            return ((long)vector._data[1] << (sizeof(int) * NC.BitsPerByte)) | (uint)vector._data[0];
        }

        public static explicit operator ulong(BitVector vector)
        {
            Debug.Assert(vector.ByteCapacity <= sizeof(ulong));

            if (vector._data.Length == 0)
            {
                return default;
            }
            if (vector._data.Length == 1)
            {
                return (uint)vector._data[0];
            }
            return ((ulong)vector._data[1] << (sizeof(uint) * NC.BitsPerByte)) | (uint)vector._data[0];
        }

        public static implicit operator string(BitVector vector)
        {
            return vector.ToString();
        }

        #endregion Explicit operators
    }
}
