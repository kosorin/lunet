using Lure.Collections;
using Lure.Net.Data;
using Lure.Net.Extensions;

namespace Lure.Net.Packets
{
    internal class ReliablePacket : NetPacket<SequencedRawMessage>, IPoolable
    {
        public ReliablePacket(ObjectPool<SequencedRawMessage> rawMessagePool) : base(rawMessagePool)
        {
        }

        public static int ChannelAckBufferLength { get; } = 128;

        public static int PacketAckBufferLength { get; } = 32;

        public SeqNo Seq { get; set; }

        public SeqNo Ack { get; set; }

        public BitVector AckBuffer { get; set; }

        void IPoolable.OnRent()
        {
        }

        void IPoolable.OnReturn()
        {
            if (Direction == NetPacketDirection.Outgoing)
            {
                // Outgoing raw messages are saved in a channel and waiting for an ack
            }
            RawMessages.Clear();
        }

        protected override void DeserializeHeaderCore(INetDataReader reader)
        {
            Seq = reader.ReadSeqNo();
            Ack = reader.ReadSeqNo();
            AckBuffer = reader.ReadBits(PacketAckBufferLength);
        }

        protected override void DeserializeDataCore(INetDataReader reader)
        {
            base.DeserializeDataCore(reader);

            RawMessages.Sort();
        }

        protected override void SerializeHeaderCore(INetDataWriter writer)
        {
            writer.WriteSeqNo(Seq);
            writer.WriteSeqNo(Ack);
            writer.WriteBits(AckBuffer);
        }
    }
}
