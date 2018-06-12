using Lure.Net.Data;
using Lure.Net.Extensions;

namespace Lure.Net.Packets
{
    internal abstract class Packet : INetSerializable
    {
        public static int SerializeCheck => 0x55555555;
        public static int AckBitsLength => 32;

        public abstract PacketType Type { get; }

        public SequenceNumber Sequence { get; set; }

        public SequenceNumber Ack { get; set; }

        public BitVector Acks { get; set; }


        void INetSerializable.Deserialize(INetDataReader reader)
        {
            // Skip reading a type - already read
            Sequence = reader.ReadSequenceNumber();
            Ack = reader.ReadSequenceNumber();
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
            writer.WriteSequenceNumber(Sequence);
            writer.WriteSequenceNumber(Ack);
            writer.WriteBits(Acks ?? new BitVector(AckBitsLength));

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
