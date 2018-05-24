using Serilog;
using System;
using System.Diagnostics;

namespace Lure.Net
{
    public class NetDataWriter
    {
        private const int ResizeData = 8;

        private static readonly ILogger Logger = Log.ForContext<NetDataWriter>();

        private byte[] _data;
        private int _length;
        private int _buffer;
        private int _bufferBitLength;

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
            EnsureTotalSize(capacity);
        }


        public byte[] Data => _data;

        public int Capacity => _data.Length;

        public int Length => _length;


        public void WriteBits(BitVector vector)
        {
            var capacity = vector.Capacity;
            if (capacity == 0)
            {
                return;
            }
            EnsureWriteSize(capacity);

            var bytes = vector.GetBytes();
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
            var fp = new FloatingPoint { Float = value };
            Write(fp.Byte0, NC.BitsPerByte);
            Write(fp.Byte1, NC.BitsPerByte);
            Write(fp.Byte2, NC.BitsPerByte);
            Write(fp.Byte3, NC.BitsPerByte);
        }

        public void WriteDouble(double value)
        {
            EnsureWriteSize(sizeof(double) * NC.BitsPerByte);
            var fp = new FloatingPoint { Double = value };
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
            if (_bufferBitLength == 0)
            {
                return;
            }

            var bitLength = NC.BitsPerByte - _bufferBitLength;
            EnsureWriteSize(bitLength);
            Write((byte)(value ? NC.Byte : NC.Zero), bitLength);
        }

        public void Flush()
        {
            if (_bufferBitLength == 0)
            {
                return;
            }
            Debug.Assert(_bufferBitLength < NC.BitsPerByte, "Complete buffer byte should be already flushed.");

            _data[_length++] = (byte)(_buffer & (NC.Byte >> (NC.BitsPerByte - _bufferBitLength)));
            _bufferBitLength = 0;
        }

        public void Reset()
        {
            _length = 0;
            _bufferBitLength = 0;
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
                Array.Copy(_data, bytes, _length);
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

            if (_bufferBitLength == 0 && bitLength == NC.BitsPerByte)
            {
                _data[_length++] = value;
                return;
            }

            _buffer |= ((value & (NC.Byte >> (NC.BitsPerByte - bitLength))) << _bufferBitLength);
            _bufferBitLength += bitLength;

            if (_bufferBitLength >= NC.BitsPerByte)
            {
                _data[_length++] = (byte)(_buffer & NC.Byte);
                _buffer >>= NC.BitsPerByte;
                _bufferBitLength -= NC.BitsPerByte;
            }
        }

        private void EnsureWriteSize(int bitLength)
        {
            var newBitLength = bitLength + _bufferBitLength + (_length * NC.BitsPerByte);
            var newLength = NetHelper.GetElementCapacity(newBitLength, NC.BitsPerByte);
            if (_data == null)
            {
                _data = new byte[newLength + ResizeData];
            }
            else if (_data.Length < newLength)
            {
                Array.Resize(ref _data, newLength + ResizeData);
            }
        }

        private void EnsureTotalSize(int capacity)
        {
            if (_data == null)
            {
                _data = new byte[capacity];
            }
            else if (_data.Length < capacity)
            {
                Array.Resize(ref _data, capacity);
            }
        }
    }
}
