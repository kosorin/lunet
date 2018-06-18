using Lure.Net.Data;
using Lure.Net.Extensions;

namespace Lure.Net.Packets
{
    internal abstract class Packet : INetSerializable
    {
        protected Packet(PacketType type, byte channel)
        {
            Type = type;
            Channel = channel;
        }

        public static int SerializeCheck => 0x55555555;

        public PacketType Type { get; }

        public byte Channel { get; }

        public PacketData Data { get; set; }


        void INetSerializable.Deserialize(INetDataReader reader)
        {
            // Skip reading a type and channel - already read and used to create a packet

            DeserializeCore(reader);

            reader.PadBits();
            if (reader.ReadInt() != SerializeCheck)
            {
                // TODO: Handle bad packets
                throw new NetException();
            }

            reader.ReadSerializable(Data);

            if (reader.Position != reader.Length)
            {
                // TODO: Handle bad packets
                throw new NetException("Remaining data in a packet.");
            }
        }

        void INetSerializable.Serialize(INetDataWriter writer)
        {
            writer.WriteByte((byte)Type);
            writer.WriteByte(Channel);

            SerializeCore(writer);

            writer.PadBits();
            writer.WriteInt(SerializeCheck);

            writer.WriteSerializable(Data);
        }

        protected abstract void DeserializeCore(INetDataReader reader);

        protected abstract void SerializeCore(INetDataWriter writer);
    }

    internal class SequencedPacket : Packet
    {
        public SequencedPacket(PacketType type, byte channel) : base(type, channel)
        {
        }

        public SeqNo Seq { get; set; }

        protected override void DeserializeCore(INetDataReader reader)
        {
            Seq = reader.ReadSeqNo();
        }

        protected override void SerializeCore(INetDataWriter writer)
        {
            writer.WriteSeqNo(Seq);
        }
    }

    internal class ReliablePacket : SequencedPacket
    {
        public ReliablePacket(PacketType type, byte channel) : base(type, channel)
        {
        }

        public static int AckBufferLength => 64;

        public static int PacketAckBufferLength => sizeof(uint) * NC.BitsPerByte;

        public SeqNo Ack { get; set; }

        public BitVector AckBuffer { get; set; }

        protected override void DeserializeCore(INetDataReader reader)
        {
            base.DeserializeCore(reader);

            Ack = reader.ReadSeqNo();
            AckBuffer = reader.ReadBits(PacketAckBufferLength);
        }

        protected override void SerializeCore(INetDataWriter writer)
        {
            base.SerializeCore(writer);

            writer.WriteSeqNo(Ack);
            writer.WriteBits(AckBuffer);
        }
    }
}
