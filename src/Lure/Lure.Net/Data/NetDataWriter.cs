using System;
using System.Diagnostics;

namespace Lure.Net.Data
{
    internal class NetDataWriter : INetDataWriter
    {
        private const int ResizeData = 8;

        private readonly bool _isShared;
        private readonly int _offset;
        private byte[] _data;
        private int _length;
        private int _capacity;
        private int _position;
        private int _bitPosition;
        private int _buffer;

        /// <summary>
        /// Initializes a new instance of the <see cref="NetDataWriter"/> class.
        /// </summary>
        public NetDataWriter()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NetDataWriter"/> class with predefined capacity.
        /// </summary>
        /// <param name="capacity">Number of allocated bytes.</param>
        public NetDataWriter(int capacity)
        {
            EnsureInitialSize(capacity);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NetDataWriter"/> class with buffer.
        /// </summary>
        public NetDataWriter(byte[] data, int offset, int length)
        {
            if (offset + length > data.Length)
            {
                throw new ArgumentOutOfRangeException("Offset + length could not be bigger than data length.");
            }

            _isShared = true;

            _data = data;
            _offset = offset;
            _capacity = length;
        }


        public int Capacity => _capacity;

        public int Length => _length;

        public int Position => _position;

        public int BitLength => _length * NC.BitsPerByte;

        public int BitPosition => (_position * NC.BitsPerByte) + _bitPosition;

        internal bool IsShared => _isShared;

        internal byte[] Data => _data;

        internal int Offset => _offset;


        public void WriteBits(BitVector vector)
        {
            var capacity = vector.Capacity;
            if (capacity == 0)
            {
                return;
            }
            EnsureWriteSize(capacity);

            var bytes = vector.GetBytes();

            if (capacity % NC.BitsPerByte == 0 && FastWrite(bytes))
            {
                return;
            }

            for (int i = 0; i < bytes.Length; i++)
            {
                Write(bytes[i], capacity > NC.BitsPerByte ? NC.BitsPerByte : capacity);
                capacity -= NC.BitsPerByte;
            }
        }

        public void WriteBytes(byte[] bytes)
        {
            var bitLength = bytes.Length * NC.BitsPerByte;
            if (bitLength == 0)
            {
                return;
            }
            EnsureWriteSize(bitLength);

            if (FastWrite(bytes))
            {
                return;
            }

            for (int i = 0; i < bytes.Length; i++)
            {
                Write(bytes[i], NC.BitsPerByte);
            }
        }

        public void WriteBit(bool value)
        {
            EnsureWriteSize(1);
            Write((byte)(value ? NC.One : NC.Zero), 1);
        }

        public void WriteBit(byte value)
        {
            EnsureWriteSize(1);
            Write((byte)(value & NC.One), 1);
        }

        public void WriteBit(int value)
        {
            EnsureWriteSize(1);
            Write((byte)(value & NC.One), 1);
        }

        public void WriteByte(byte value)
        {
            EnsureWriteSize(NC.BitsPerByte);
            Write(value, NC.BitsPerByte);
        }

        public void WriteSByte(sbyte value)
        {
            EnsureWriteSize(NC.BitsPerByte);
            Write((byte)value, NC.BitsPerByte);
        }

        public void WriteShort(short value)
        {
            EnsureWriteSize(sizeof(short) * NC.BitsPerByte);
            Write((byte)(value >> (0 * NC.BitsPerByte)), NC.BitsPerByte);
            Write((byte)(value >> (1 * NC.BitsPerByte)), NC.BitsPerByte);
        }

        public void WriteUShort(ushort value)
        {
            EnsureWriteSize(sizeof(ushort) * NC.BitsPerByte);
            Write((byte)(value >> (0 * NC.BitsPerByte)), NC.BitsPerByte);
            Write((byte)(value >> (1 * NC.BitsPerByte)), NC.BitsPerByte);
        }

        public void WriteInt(int value)
        {
            EnsureWriteSize(sizeof(int) * NC.BitsPerByte);
            Write((byte)(value >> (0 * NC.BitsPerByte)), NC.BitsPerByte);
            Write((byte)(value >> (1 * NC.BitsPerByte)), NC.BitsPerByte);
            Write((byte)(value >> (2 * NC.BitsPerByte)), NC.BitsPerByte);
            Write((byte)(value >> (3 * NC.BitsPerByte)), NC.BitsPerByte);
        }

        public void WriteUInt(uint value)
        {
            EnsureWriteSize(sizeof(uint) * NC.BitsPerByte);
            Write((byte)(value >> (0 * NC.BitsPerByte)), NC.BitsPerByte);
            Write((byte)(value >> (1 * NC.BitsPerByte)), NC.BitsPerByte);
            Write((byte)(value >> (2 * NC.BitsPerByte)), NC.BitsPerByte);
            Write((byte)(value >> (3 * NC.BitsPerByte)), NC.BitsPerByte);
        }

        public void WriteLong(long value)
        {
            EnsureWriteSize(sizeof(long) * NC.BitsPerByte);
            Write((byte)(value >> (0 * NC.BitsPerByte)), NC.BitsPerByte);
            Write((byte)(value >> (1 * NC.BitsPerByte)), NC.BitsPerByte);
            Write((byte)(value >> (2 * NC.BitsPerByte)), NC.BitsPerByte);
            Write((byte)(value >> (3 * NC.BitsPerByte)), NC.BitsPerByte);
            Write((byte)(value >> (4 * NC.BitsPerByte)), NC.BitsPerByte);
            Write((byte)(value >> (5 * NC.BitsPerByte)), NC.BitsPerByte);
            Write((byte)(value >> (6 * NC.BitsPerByte)), NC.BitsPerByte);
            Write((byte)(value >> (7 * NC.BitsPerByte)), NC.BitsPerByte);
        }

        public void WriteULong(ulong value)
        {
            EnsureWriteSize(sizeof(ulong) * NC.BitsPerByte);
            Write((byte)(value >> (0 * NC.BitsPerByte)), NC.BitsPerByte);
            Write((byte)(value >> (1 * NC.BitsPerByte)), NC.BitsPerByte);
            Write((byte)(value >> (2 * NC.BitsPerByte)), NC.BitsPerByte);
            Write((byte)(value >> (3 * NC.BitsPerByte)), NC.BitsPerByte);
            Write((byte)(value >> (4 * NC.BitsPerByte)), NC.BitsPerByte);
            Write((byte)(value >> (5 * NC.BitsPerByte)), NC.BitsPerByte);
            Write((byte)(value >> (6 * NC.BitsPerByte)), NC.BitsPerByte);
            Write((byte)(value >> (7 * NC.BitsPerByte)), NC.BitsPerByte);
        }

        public void WriteFloat(float value)
        {
            EnsureWriteSize(sizeof(float) * NC.BitsPerByte);
            var fp = new FloatingPointConverter { Float = value };
            Write(fp.Byte0, NC.BitsPerByte);
            Write(fp.Byte1, NC.BitsPerByte);
            Write(fp.Byte2, NC.BitsPerByte);
            Write(fp.Byte3, NC.BitsPerByte);
        }

        public void WriteDouble(double value)
        {
            EnsureWriteSize(sizeof(double) * NC.BitsPerByte);
            var fp = new FloatingPointConverter { Double = value };
            Write(fp.Byte0, NC.BitsPerByte);
            Write(fp.Byte1, NC.BitsPerByte);
            Write(fp.Byte2, NC.BitsPerByte);
            Write(fp.Byte3, NC.BitsPerByte);
            Write(fp.Byte4, NC.BitsPerByte);
            Write(fp.Byte5, NC.BitsPerByte);
            Write(fp.Byte6, NC.BitsPerByte);
            Write(fp.Byte7, NC.BitsPerByte);
        }


        public void PadBits()
        {
            PadBits(false);
        }

        public void PadBits(bool value)
        {
            if (_bitPosition == 0)
            {
                return;
            }

            var bitLength = NC.BitsPerByte - _bitPosition;
            EnsureWriteSize(bitLength);
            Write((byte)(value ? NC.Byte : NC.Zero), bitLength);
        }

        public void Flush()
        {
            PadBits(false);
        }

        public void Reset()
        {
            _length = 0;
            _position = 0;
            _bitPosition = 0;
        }

        public byte[] GetBytes(bool flush = true)
        {
            if (flush)
            {
                Flush();
            }

            var bytes = new byte[_length];
            if (_length > 0)
            {
                Array.Copy(_data, _offset, bytes, 0, _length);
            }
            return bytes;
        }


        private void Write(byte value, int bitLength)
        {
            if (bitLength == 0)
            {
                return;
            }
            Debug.Assert(bitLength >= 0 && bitLength <= NC.BitsPerByte);

            if (_bitPosition == 0 && bitLength == NC.BitsPerByte)
            {
                _data[_offset + _position++] = value;
                if (_length < _position)
                {
                    _length = _position;
                }
                return;
            }

            _buffer |= ((value & (NC.Byte >> (NC.BitsPerByte - bitLength))) << _bitPosition);
            _bitPosition += bitLength;

            if (_bitPosition >= NC.BitsPerByte)
            {
                _data[_offset + _position++] = (byte)(_buffer & NC.Byte);
                if (_length < _position)
                {
                    _length = _position;
                }
                _buffer >>= NC.BitsPerByte;
                _bitPosition -= NC.BitsPerByte;
            }
        }

        private bool FastWrite(byte[] bytes)
        {
            if (_bitPosition == 0)
            {
                Array.Copy(bytes, 0, _data, _position, bytes.Length);
                _position += bytes.Length;
                if (_length < _position)
                {
                    _length = _position;
                }
                return true;
            }
            return false;
        }

        private void EnsureWriteSize(int bitLength)
        {
            var newBitLength = bitLength + _bitPosition + (_position * NC.BitsPerByte);
            var newLength = NetHelper.GetElementCapacity(newBitLength, NC.BitsPerByte);

            if (_isShared)
            {
                if (_capacity < newLength)
                {
                    throw new InvalidOperationException();
                }
                return;
            }

            if (_data == null)
            {
                _capacity = newLength + ResizeData;
                _data = new byte[_capacity];
            }
            else if (_capacity < newLength)
            {
                _capacity = newLength + ResizeData;
                Array.Resize(ref _data, _capacity);
            }
        }

        private void EnsureInitialSize(int length)
        {
            if (_isShared)
            {
                if (_capacity < length)
                {
                    throw new InvalidOperationException();
                }
                return;
            }

            if (_data == null)
            {
                _capacity = length;
                _data = new byte[_capacity];
            }
            else if (_capacity < length)
            {
                _capacity = length;
                Array.Resize(ref _data, _capacity);
            }
        }
    }
}
