using Serilog;
using System;
using System.Diagnostics;

namespace Lure.Net.Data
{
    internal class NetDataReader : INetDataReader
    {
        private readonly bool _isShared;
        private readonly byte[] _data;
        private readonly int _offset;
        private readonly int _length;
        private int _position;
        private int _bitPosition;

        public NetDataReader(byte[] data)
        {
            _data = data;
            _offset = 0;
            _length = data.Length;
        }

        public NetDataReader(byte[] data, int offset, int length)
        {
            if (offset + length > data.Length)
            {
                throw new ArgumentOutOfRangeException("Offset + length could not be bigger than data length.");
            }

            _isShared = true;

            _data = data;
            _offset = offset;
            _length = length;
        }


        public int Length => _length;

        public int Position => _position;

        public int BitLength => _length * NC.BitsPerByte;

        public int BitPosition => (_position * NC.BitsPerByte) + _bitPosition;

        internal bool IsShared => _isShared;

        internal byte[] Data => _data;

        internal int Offset => _offset;


        public BitVector ReadBits(int bitLength)
        {
            if (bitLength < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bitLength));
            }
            if (bitLength == 0)
            {
                return new BitVector(bitLength);
            }
            EnsureReadSize(bitLength);

            var bytes = new byte[NetHelper.GetElementCapacity(bitLength, NC.BitsPerByte)];
            for (int i = 0, capacity = bitLength; i < bytes.Length; i++, capacity -= NC.BitsPerByte)
            {
                bytes[i] = Read(capacity > NC.BitsPerByte ? NC.BitsPerByte : capacity);
            }
            return new BitVector(bytes, bitLength);
        }

        public byte[] ReadBytes(int length)
        {
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }
            if (length == 0)
            {
                return new byte[length];
            }
            EnsureReadSize(length * NC.BitsPerByte);

            var bytes = new byte[length];
            if (FastRead(bytes))
            {
                return bytes;
            }

            for (int i = 0; i < length; i++)
            {
                bytes[i] = Read(NC.BitsPerByte);
            }
            return bytes;
        }

        public byte[] ReadBytesToEnd()
        {
            return ReadBytes(_length - _position);
        }

        public bool ReadBit()
        {
            EnsureReadSize(1);
            return (Read(1) & NC.One) == NC.One;
        }

        public byte ReadByte()
        {
            EnsureReadSize(NC.BitsPerByte);
            return Read(NC.BitsPerByte);
        }

        public sbyte ReadSByte()
        {
            EnsureReadSize(NC.BitsPerByte);
            return (sbyte)Read(NC.BitsPerByte);
        }

        public short ReadShort()
        {
            EnsureReadSize(sizeof(short) * NC.BitsPerByte);
            ushort value = Read(NC.BitsPerByte);
            value |= (ushort)(Read(NC.BitsPerByte) << (1 * NC.BitsPerByte));
            return (short)value;
        }

        public ushort ReadUShort()
        {
            EnsureReadSize(sizeof(ushort) * NC.BitsPerByte);
            ushort value = Read(NC.BitsPerByte);
            value |= (ushort)(Read(NC.BitsPerByte) << (1 * NC.BitsPerByte));
            return value;
        }

        public int ReadInt()
        {
            EnsureReadSize(sizeof(int) * NC.BitsPerByte);
            uint value = Read(NC.BitsPerByte);
            value |= ((uint)Read(NC.BitsPerByte) << (1 * NC.BitsPerByte));
            value |= ((uint)Read(NC.BitsPerByte) << (2 * NC.BitsPerByte));
            value |= ((uint)Read(NC.BitsPerByte) << (3 * NC.BitsPerByte));
            return (int)value;
        }

        public uint ReadUInt()
        {
            EnsureReadSize(sizeof(uint) * NC.BitsPerByte);
            uint value = Read(NC.BitsPerByte);
            value |= ((uint)Read(NC.BitsPerByte) << (1 * NC.BitsPerByte));
            value |= ((uint)Read(NC.BitsPerByte) << (2 * NC.BitsPerByte));
            value |= ((uint)Read(NC.BitsPerByte) << (3 * NC.BitsPerByte));
            return value;
        }

        public long ReadLong()
        {
            EnsureReadSize(sizeof(long) * NC.BitsPerByte);
            ulong value = Read(NC.BitsPerByte);
            value |= ((ulong)Read(NC.BitsPerByte) << (1 * NC.BitsPerByte));
            value |= ((ulong)Read(NC.BitsPerByte) << (2 * NC.BitsPerByte));
            value |= ((ulong)Read(NC.BitsPerByte) << (3 * NC.BitsPerByte));
            value |= ((ulong)Read(NC.BitsPerByte) << (4 * NC.BitsPerByte));
            value |= ((ulong)Read(NC.BitsPerByte) << (5 * NC.BitsPerByte));
            value |= ((ulong)Read(NC.BitsPerByte) << (6 * NC.BitsPerByte));
            value |= ((ulong)Read(NC.BitsPerByte) << (7 * NC.BitsPerByte));
            return (long)value;
        }

        public ulong ReadULong()
        {
            EnsureReadSize(sizeof(ulong) * NC.BitsPerByte);
            ulong value = Read(NC.BitsPerByte);
            value |= ((ulong)Read(NC.BitsPerByte) << (1 * NC.BitsPerByte));
            value |= ((ulong)Read(NC.BitsPerByte) << (2 * NC.BitsPerByte));
            value |= ((ulong)Read(NC.BitsPerByte) << (3 * NC.BitsPerByte));
            value |= ((ulong)Read(NC.BitsPerByte) << (4 * NC.BitsPerByte));
            value |= ((ulong)Read(NC.BitsPerByte) << (5 * NC.BitsPerByte));
            value |= ((ulong)Read(NC.BitsPerByte) << (6 * NC.BitsPerByte));
            value |= ((ulong)Read(NC.BitsPerByte) << (7 * NC.BitsPerByte));
            return value;
        }

        public float ReadFloat()
        {
            var fp = new FloatingPointConverter { UInt = ReadUInt() };
            return fp.Float;
        }

        public double ReadDouble()
        {
            var fp = new FloatingPointConverter { ULong = ReadULong() };
            return fp.Double;
        }


        public void PadBits()
        {
            if (_bitPosition == 0)
            {
                return;
            }

            EnsureReadSize(NC.BitsPerByte - _bitPosition);
            _position++;
            _bitPosition = 0;
        }

        public void Seek()
        {
            _position = 0;
            _bitPosition = 0;
        }

        public void Seek(int bitPosition)
        {
            if (bitPosition < 0 || BitLength < bitPosition)
            {
                throw new ArgumentOutOfRangeException(nameof(bitPosition));
            }
            _position = bitPosition / NC.BitsPerByte;
            _bitPosition = bitPosition % NC.BitsPerByte;
        }


        private byte Read(int bitLength)
        {
            if (bitLength == 0)
            {
                return NC.Zero;
            }
            Debug.Assert(bitLength >= 0 && bitLength <= NC.BitsPerByte);

            if (_bitPosition == 0 && bitLength == NC.BitsPerByte)
            {
                return _data[_offset + _position++];
            }

            byte value;

            var inFirst = NC.BitsPerByte - _bitPosition;
            value = (byte)((_data[_offset + _position] >> _bitPosition) & (NC.Byte >> (NC.BitsPerByte - bitLength)));

            if (inFirst < bitLength)
            {
                _position++;

                var inSecond = bitLength - inFirst;
                value |= (byte)((_data[_offset + _position] & (NC.Byte >> (NC.BitsPerByte - inSecond))) << inFirst);

                _bitPosition = inSecond;
            }
            else
            {
                _bitPosition += bitLength;
                _bitPosition %= NC.BitsPerByte;
            }

            return value;
        }

        private bool FastRead(byte[] bytes)
        {
            if (_bitPosition == 0)
            {
                Array.Copy(_data, _position, bytes, 0, bytes.Length);
                _position += bytes.Length;
                return true;
            }
            return false;
        }

        private void EnsureReadSize(int bitLength)
        {
            var actual = BitLength;
            var required = BitPosition + bitLength;
            if (actual < required)
            {
                throw new InvalidOperationException($"Not enough data. Required = {required}, Actual = {actual}.");
            }
        }
    }
}
