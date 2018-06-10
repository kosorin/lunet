using Lure.Net.Data;

namespace Lure.Net.Packets
{
    internal abstract class Packet : INetSerializable
    {
        public static int SerializeCheck => 0x55555555;
        public static int AckBitsLength => 32;

        public abstract PacketType Type { get; }

        public ushort Sequence { get; set; }

        public ushort Ack { get; set; }

        public BitVector Acks { get; set; }


        void INetSerializable.Deserialize(INetDataReader reader)
        {
            // Skip reading a type - already read
            Sequence = reader.ReadUShort();
            Ack = reader.ReadUShort();
            Acks = reader.ReadBits(AckBitsLength);

            if (reader.ReadInt() != SerializeCheck)
            {
                // TODO: Handle bad packets
                throw new NetException();
            }
            reader.PadBits();

            DeserializeCore(reader);

            if (reader.Position != reader.Length)
            {
                // TODO: Handle bad packets
                throw new NetException();
            }
        }

        void INetSerializable.Serialize(INetDataWriter writer)
        {
            writer.WriteByte((byte)Type);
            writer.WriteUShort(Sequence);
            writer.WriteUShort(Ack);
            writer.WriteBits(Acks ?? new BitVector(AckBitsLength));

            writer.WriteInt(SerializeCheck);
            writer.PadBits();

            SerializeCore(writer);
        }

        protected abstract void DeserializeCore(INetDataReader reader);

        protected abstract void SerializeCore(INetDataWriter writer);
    }
}
