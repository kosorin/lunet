namespace Lure.Net.Data
{
    public interface INetDataWriter
    {
        int Capacity { get; }

        int Length { get; }

        int Position { get; }

        int BitLength { get; }

        int BitPosition { get; }


        void WriteBits(BitVector vector);

        void WriteBytes(byte[] bytes);

        void WriteBit(bool value);

        void WriteBit(byte value);

        void WriteBit(int value);

        void WriteByte(byte value);

        void WriteSByte(sbyte value);

        void WriteShort(short value);

        void WriteUShort(ushort value);

        void WriteInt(int value);

        void WriteUInt(uint value);

        void WriteLong(long value);

        void WriteULong(ulong value);

        void WriteFloat(float value);

        void WriteDouble(double value);


        void PadBits();

        void PadBits(bool value);

        void Flush();

        void Reset();

        byte[] GetBytes(bool flush = true);
    }
}
