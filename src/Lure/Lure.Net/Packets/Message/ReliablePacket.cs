using Lure.Collections;
using Lure.Net.Data;
using Lure.Net.Extensions;

namespace Lure.Net.Packets.Message
{
    internal class ReliablePacket : MessagePacket<ReliableRawMessage>
    {
        public ReliablePacket(ObjectPool<ReliableRawMessage> rawMessagePool) : base(rawMessagePool)
        {
        }

        public static int AckBufferLength { get; } = 64;

        public static int PacketAckBufferLength { get; } = sizeof(uint) * NC.BitsPerByte;

        public SeqNo Seq { get; set; }

        public SeqNo Ack { get; set; }

        public BitVector AckBuffer { get; set; }

        protected override void DeserializeHeaderCore(INetDataReader reader)
        {
            Seq = reader.ReadSeqNo();
            Ack = reader.ReadSeqNo();
            AckBuffer = reader.ReadBits(PacketAckBufferLength);
        }

        protected override void SerializeHeaderCore(INetDataWriter writer)
        {
            writer.WriteSeqNo(Seq);
            writer.WriteSeqNo(Ack);
            writer.WriteBits(AckBuffer);
        }
    }
}
