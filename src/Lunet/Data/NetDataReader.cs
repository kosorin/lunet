using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;

namespace Lunet.Data
{
    public class NetDataReader : NetDataBuffer
    {
        private int _readOffset;
        private int _readLength;
        private int _readPosition;
        private int _readBitPosition;

        public NetDataReader(byte[] data)
            : this(data, 0, data.Length)
        {
        }

        public NetDataReader(byte[] data, int offset, int length)
            : base(data, offset, length)
        {
            Reset();
        }

        public override int Offset => DataOffset + _readOffset;

        public override int Length => _readLength;

        public override int Position => _readPosition;

        public void Reset()
        {
            Reset(DataOffset, DataLength);
        }

        public void Reset(int length)
        {
            Reset(0, length);
        }

        public void Reset(int offset, int length)
        {
            if (offset < DataOffset || DataOffset + DataLength < offset)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }
            if (length < 0 || DataOffset + DataLength < offset + length)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            _readOffset = offset - DataOffset;
            _readLength = length;
            _readPosition = 0;
            _readBitPosition = 0;
        }

        public void ResetRelative(int left, int right)
        {
            Reset(Offset + left, Length - left - right);
        }

        public void Seek(int bitOffset, SeekOrigin origin)
        {
            var relativeBitPosition = origin switch
            {
                SeekOrigin.Begin => (0 - GetReadBitPosition()) + bitOffset,
                SeekOrigin.End => (GetReadBitLength() - GetReadBitPosition()) - bitOffset,
                _ => bitOffset,
            };
            ThrowIfNotEnoughData(relativeBitPosition);

            var newBitPosition = GetReadBitPosition() + relativeBitPosition;
            _readPosition = newBitPosition / NC.BitsPerByte;
            _readBitPosition = newBitPosition % NC.BitsPerByte;
        }

        public void PadByte()
        {
            if (_readBitPosition == 0)
            {
                return;
            }

            ThrowIfNotEnoughData(NC.BitsPerByte - _readBitPosition);
            _readPosition++;
            _readBitPosition = 0;
        }

        public void SkipBits(int bitCount)
        {
            if (bitCount == 0)
            {
                return;
            }

            ThrowIfNotEnoughData(bitCount);
            _readBitPosition += bitCount;
            _readPosition += _readBitPosition / NC.BitsPerByte;
            _readBitPosition %= NC.BitsPerByte;
        }

        public void SkipBytes(int count)
        {
            if (count == 0)
            {
                return;
            }
            if (count < 0 || _readPosition + count > _readLength)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            ThrowIfNotEnoughData(count * NC.BitsPerByte);
            _readPosition += count;
        }

        public BitVector ReadBits(int bitCount)
        {
            if (bitCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bitCount));
            }
            if (bitCount == 0)
            {
                return new BitVector(bitCount);
            }

            ThrowIfNotEnoughData(bitCount);
            var bytes = new byte[NetHelper.GetElementCapacity(bitCount, NC.BitsPerByte)];
            for (int i = 0, capacity = bitCount; i < bytes.Length; i++, capacity -= NC.BitsPerByte)
            {
                bytes[i] = Read(capacity > NC.BitsPerByte ? NC.BitsPerByte : capacity);
            }
            return new BitVector(bytes, bitCount);
        }

        public byte[] ReadBytes()
        {
            return ReadBytes(_readLength - _readPosition);
        }

        public byte[] ReadBytes(int count)
        {
            return ReadSpan(count).ToArray();
        }

        public ReadOnlySpan<byte> ReadSpan()
        {
            return ReadSpan(_readLength - _readPosition);
        }

        public ReadOnlySpan<byte> ReadSpan(int count)
        {
            if (_readBitPosition != 0)
            {
                throw new InvalidOperationException($"Bit position must be divisible by {NC.BitsPerByte}. Can read only full bytes.");
            }
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            ThrowIfNotEnoughData(count * NC.BitsPerByte);
            var span = new ReadOnlySpan<byte>(Data, Offset + Position, count);
            _readPosition += count;
            return span;
        }

        public bool ReadBit()
        {
            ThrowIfNotEnoughData(1);
            return (Read(1) & NC.One) == NC.One;
        }

        public byte ReadByte()
        {
            ThrowIfNotEnoughData(NC.BitsPerByte);
            return Read(NC.BitsPerByte);
        }

        public sbyte ReadSByte()
        {
            ThrowIfNotEnoughData(NC.BitsPerByte);
            return (sbyte)Read(NC.BitsPerByte);
        }

        public short ReadShort()
        {
            ThrowIfNotEnoughData(sizeof(short) * NC.BitsPerByte);
            ushort value = Read(NC.BitsPerByte);
            value |= (ushort)(Read(NC.BitsPerByte) << (1 * NC.BitsPerByte));
            return (short)value;
        }

        public ushort ReadUShort()
        {
            ThrowIfNotEnoughData(sizeof(ushort) * NC.BitsPerByte);
            ushort value = Read(NC.BitsPerByte);
            value |= (ushort)(Read(NC.BitsPerByte) << (1 * NC.BitsPerByte));
            return value;
        }

        public int ReadInt()
        {
            ThrowIfNotEnoughData(sizeof(int) * NC.BitsPerByte);
            uint value = Read(NC.BitsPerByte);
            value |= ((uint)Read(NC.BitsPerByte) << (1 * NC.BitsPerByte));
            value |= ((uint)Read(NC.BitsPerByte) << (2 * NC.BitsPerByte));
            value |= ((uint)Read(NC.BitsPerByte) << (3 * NC.BitsPerByte));
            return (int)value;
        }

        public uint ReadUInt()
        {
            ThrowIfNotEnoughData(sizeof(uint) * NC.BitsPerByte);
            uint value = Read(NC.BitsPerByte);
            value |= ((uint)Read(NC.BitsPerByte) << (1 * NC.BitsPerByte));
            value |= ((uint)Read(NC.BitsPerByte) << (2 * NC.BitsPerByte));
            value |= ((uint)Read(NC.BitsPerByte) << (3 * NC.BitsPerByte));
            return value;
        }

        public long ReadLong()
        {
            ThrowIfNotEnoughData(sizeof(long) * NC.BitsPerByte);
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
            ThrowIfNotEnoughData(sizeof(ulong) * NC.BitsPerByte);
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

        public SeqNo ReadSeqNo()
        {
            return new SeqNo(ReadUShort());
        }

        private byte Read(int bitCount)
        {
            Debug.Assert(bitCount >= 0 && bitCount <= NC.BitsPerByte);

            byte value;

            if (bitCount == 0)
            {
                value = NC.Zero;
                return value;
            }
            if (_readBitPosition == 0 && bitCount == NC.BitsPerByte)
            {
                value = Data[Offset + Position];
                _readPosition++;
                return value;
            }

            var bitsFromFirstByte = NC.BitsPerByte - _readBitPosition;
            value = (byte)((Data[Offset + Position] >> _readBitPosition) & (NC.Byte >> (NC.BitsPerByte - bitCount)));

            if (bitsFromFirstByte < bitCount)
            {
                _readPosition++;

                var bitsFromSecondByte = bitCount - bitsFromFirstByte;
                value |= (byte)((Data[Offset + Position] & (NC.Byte >> (NC.BitsPerByte - bitsFromSecondByte))) << bitsFromFirstByte);

                _readBitPosition = bitsFromSecondByte;
            }
            else
            {
                _readBitPosition += bitCount;
                _readBitPosition %= NC.BitsPerByte;
            }

            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetReadBitLength()
        {
            return _readLength * NC.BitsPerByte;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetReadBitPosition()
        {
            return (_readPosition * NC.BitsPerByte) + _readBitPosition;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ThrowIfNotEnoughData(int bitCount)
        {
            var actual = GetReadBitLength();
            var required = GetReadBitPosition() + bitCount;
            if (actual < required)
            {
                throw new InvalidOperationException($"Not enough data. Required = {required}, Actual = {actual}.");
            }
        }
    }
}
