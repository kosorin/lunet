using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Lunet.Data
{
    public class NetDataWriter : DataBuffer
    {
        private const int DefaultDataLength = 8;

        private int _writeOffset;
        private int _writeLength;
        private int _writePosition;
        private int _writeBitPosition;
        private int _buffer;

        public NetDataWriter()
            : base(DefaultDataLength)
        {
        }

        public NetDataWriter(int length)
            : base(length)
        {
            Reset();
        }

        public NetDataWriter(byte[] data)
            : this(data, 0, data.Length)
        {
        }

        public NetDataWriter(byte[] data, int offset, int length)
            : base(data, offset, length)
        {
            Reset();
        }

        public override int Offset => BufferOffset + _writeOffset;

        public override int Length => _writeLength;

        public override int Position => _writePosition;

        public override byte[] GetBytes()
        {
            Flush();
            return base.GetBytes();
        }

        public override ReadOnlySpan<byte> GetSpan()
        {
            Flush();
            return base.GetSpan();
        }

        public void Reset()
        {
            Reset(0);
        }

        public void Reset(int writeOffset)
        {
            if (BufferLength < writeOffset)
            {
                throw new ArgumentOutOfRangeException(nameof(writeOffset));
            }

            _writeOffset = writeOffset;
            _writeLength = 0;
            _writePosition = 0;
            _writeBitPosition = 0;
            _buffer = 0;
        }

        public void Flush()
        {
            PadByte();
        }

        public void PadByte()
        {
            PadByte(false);
        }

        public void PadByte(bool value)
        {
            if (_writeBitPosition == 0)
            {
                return;
            }

            var bitCount = NC.BitsPerByte - _writeBitPosition;
            EnsureWriteSize(bitCount);
            Write(value ? NC.Byte : NC.Zero, bitCount);
        }

        public void WriteBits(BitVector vector)
        {
            var bitCount = vector.Capacity;
            if (bitCount <= 0)
            {
                return;
            }

            EnsureWriteSize(bitCount);

            var bytes = vector.ToBytes();

            if (bitCount % NC.BitsPerByte == 0 && FastWrite(bytes))
            {
                return;
            }

            for (var i = 0; i < bytes.Length; i++)
            {
                Write(bytes[i], bitCount > NC.BitsPerByte ? NC.BitsPerByte : bitCount);
                bitCount -= NC.BitsPerByte;
            }
        }

        public void WriteBytes(byte[] bytes)
        {
            var count = bytes.Length;
            if (count <= 0)
            {
                return;
            }

            EnsureWriteSize(count * NC.BitsPerByte);

            if (FastWrite(bytes))
            {
                return;
            }

            for (var i = 0; i < count; i++)
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

        public void WriteSeqNo(SeqNo seq)
        {
            WriteUShort(seq.Value);
        }

        private void Write(byte value, int bitCount)
        {
            if (bitCount == 0)
            {
                return;
            }
            Debug.Assert(bitCount >= 0 && bitCount <= NC.BitsPerByte);

            if (FastWrite(value, bitCount))
            {
                return;
            }

            _buffer |= ((value & (NC.Byte >> (NC.BitsPerByte - bitCount))) << _writeBitPosition);
            _writeBitPosition += bitCount;

            if (_writeBitPosition >= NC.BitsPerByte)
            {
                Data[Offset + _writePosition++] = (byte)(_buffer & NC.Byte);
                if (_writeLength < _writePosition)
                {
                    _writeLength = _writePosition;
                }
                _buffer >>= NC.BitsPerByte;
                _writeBitPosition -= NC.BitsPerByte;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool FastWrite(byte value, int bitCount)
        {
            if (_writeBitPosition != 0 || bitCount != NC.BitsPerByte)
            {
                return false;
            }

            Data[Offset + _writePosition++] = value;
            if (_writeLength < _writePosition)
            {
                _writeLength = _writePosition;
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool FastWrite(byte[] bytes)
        {
            if (_writeBitPosition != 0)
            {
                return false;
            }

            Array.Copy(bytes, 0, Data, Offset + _writePosition, bytes.Length);
            _writePosition += bytes.Length;
            if (_writeLength < _writePosition)
            {
                _writeLength = _writePosition;
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetWriteBitLength()
        {
            return _writeLength * NC.BitsPerByte;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetWriteBitPosition()
        {
            return (_writePosition * NC.BitsPerByte) + _writeBitPosition;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureWriteSize(int bitCount)
        {
            var newWriteBitLength = GetWriteBitPosition() + bitCount;
            var newWriteLength = NetHelper.GetElementCapacity(newWriteBitLength, NC.BitsPerByte);
            EnsureSize(_writeOffset + newWriteLength);
        }
    }
}
