using Lure.Net.Data;
using Lure.Net.Extensions;

namespace Lure.Net.Packets
{
    internal abstract class Packet : INetSerializable
    {
        public static int SerializeCheck => 0x55555555;
        public static int AcksLength => 32;

        protected Packet(PacketType type)
        {
            Type = type;
        }

        public PacketType Type { get; }

        public SeqNo Seq { get; set; }

        public SeqNo Ack { get; set; }

        public BitVector AckBuffer { get; set; }


        void INetSerializable.Deserialize(INetDataReader reader)
        {
            // Skip reading a type - already read and used to create packet
            Seq = reader.ReadSeqNo();
            Ack = reader.ReadSeqNo();
            AckBuffer = reader.ReadBits(AcksLength);

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
            writer.WriteSeqNo(Seq);
            writer.WriteSeqNo(Ack);
            writer.WriteBits(AckBuffer);

            writer.WriteInt(SerializeCheck);
            writer.PadBits();

            SerializeCore(writer);
        }

        protected virtual void DeserializeCore(INetDataReader reader)
        {
        }

        protected virtual void SerializeCore(INetDataWriter writer)
        {
        }
    }
}
