using Serilog;
using System;
using System.Diagnostics;

namespace Lure.Net
{
    public class NetDataReader
    {
        private static readonly ILogger Logger = Log.ForContext<NetDataReader>();

        private readonly byte[] _data;
        private int _position;
        private int _bitOffset;

        public NetDataReader(byte[] data)
        {
            _data = data;
        }


        public int BitLength => _data.Length * NC.BitsPerByte;

        public int BitPosition => (_position * NC.BitsPerByte) + _bitOffset;

        public int Length => _data.Length;

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
            for (int i = 0; i < length; i++)
            {
                bytes[i] = Read(NC.BitsPerByte);
            }
            return bytes;
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
            var fp = new FloatingPoint { UInt = ReadUInt() };
            return fp.Float;
        }

        public double ReadDouble()
        {
            var fp = new FloatingPoint { ULong = ReadULong() };
            return fp.Double;
        }


        public void PadBits()
        {
            if (_bitOffset == 0)
            {
                return;
            }

            EnsureReadSize(NC.BitsPerByte - _bitOffset);
            _position++;
            _bitOffset = 0;
        }

        public void Seek(int bitPosition)
        {
            if (bitPosition < 0 || BitLength < bitPosition)
            {
                throw new ArgumentOutOfRangeException(nameof(bitPosition));
            }
            _position = bitPosition / NC.BitsPerByte;
            _bitOffset = bitPosition % NC.BitsPerByte;
        }

        public void Reset()
        {
            _position = 0;
            _bitOffset = 0;
        }


        private byte Read(int bitLength)
        {
            if (bitLength == 0)
            {
                return NC.Zero;
            }
            Debug.Assert(bitLength >= 0 && bitLength <= NC.BitsPerByte);

            if (_bitOffset == 0 && bitLength == NC.BitsPerByte)
            {
                return _data[_position++];
            }

            byte value;

            var inFirst = NC.BitsPerByte - _bitOffset;
            value = (byte)((_data[_position] >> _bitOffset) & (NC.Byte >> (NC.BitsPerByte - bitLength)));

            if (inFirst < bitLength)
            {
                _position++;

                var inSecond = bitLength - inFirst;
                value |= (byte)((_data[_position] & (NC.Byte >> (NC.BitsPerByte - inSecond))) << inFirst);

                _bitOffset = inSecond;
            }
            else
            {
                _bitOffset += bitLength;
                _bitOffset %= NC.BitsPerByte;
            }

            return value;
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
