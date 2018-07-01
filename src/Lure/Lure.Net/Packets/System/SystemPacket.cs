using Lure.Net.Data;
using Lure.Net.Extensions;

namespace Lure.Net.Packets.System
{
    internal abstract class SystemPacket : Packet
    {
        public static int ChannelAckBufferLength { get; } = 32;

        public static int PacketAckBufferLength { get; } = 16;

        public SystemPacketType Type { get; set; }

        public SeqNo Seq { get; set; }

        public SeqNo Ack { get; set; }

        public BitVector AckBuffer { get; set; }

        public long? Timestamp { get; set; }

        protected sealed override void DeserializeHeaderCore(INetDataReader reader)
        {
            // Skip reading a type - already read and used to create a message

            Seq = reader.ReadSeqNo();
            Ack = reader.ReadSeqNo();
            AckBuffer = reader.ReadBits(PacketAckBufferLength);
        }

        protected sealed override void SerializeHeaderCore(INetDataWriter writer)
        {
            writer.WriteByte((byte)Type);

            writer.WriteSeqNo(Seq);
            writer.WriteSeqNo(Ack);
            writer.WriteBits(AckBuffer);
        }
    }
}
