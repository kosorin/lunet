namespace Lure.Net.Data
{
    public interface INetDataReader
    {
        int Length { get; }

        int Position { get; }

        int BitLength { get; }

        int BitPosition { get; }


        BitVector ReadBits(int bitLength);

        byte[] ReadBytes(int length);

        byte[] ReadBytesToEnd();

        bool ReadBit();

        byte ReadByte();

        sbyte ReadSByte();

        short ReadShort();

        ushort ReadUShort();

        int ReadInt();

        uint ReadUInt();

        long ReadLong();

        ulong ReadULong();

        float ReadFloat();

        double ReadDouble();


        void PadBits();

        void Seek();

        void Seek(int bitPosition);

        void SkipBits(int count);

        void SkipBytes(int count);
    }
}
