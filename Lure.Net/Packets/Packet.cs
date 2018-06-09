using Lure.Net.Data;

namespace Lure.Net.Packets
{
    internal abstract class Packet : INetSerializable
    {
        public static int SerializeCheck => 0x55555555;

        public abstract PacketType Type { get; }

        public ushort Sequence { get; set; }

        public ushort Ack { get; set; }

        public uint AckBits { get; set; }


        public void Deserialize(INetDataReader reader)
        {
            // Skip reading a type - already read
            Sequence = reader.ReadUShort();
            Ack = reader.ReadUShort();
            AckBits = reader.ReadUInt();

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

        public void Serialize(INetDataWriter writer)
        {
            writer.WriteByte((byte)Type);
            writer.WriteUShort(Sequence);
            writer.WriteUShort(Ack);
            writer.WriteUInt(AckBits);

            writer.WriteInt(SerializeCheck);
            writer.PadBits();

            SerializeCore(writer);
        }

        protected abstract void DeserializeCore(INetDataReader reader);

        protected abstract void SerializeCore(INetDataWriter writer);
    }
}
