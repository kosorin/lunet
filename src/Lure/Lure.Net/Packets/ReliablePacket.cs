using Lure.Net.Data;
using Lure.Net.Extensions;
using System;

namespace Lure.Net.Packets
{
    internal class ReliablePacket : NetPacket<SequencedRawMessage>
    {
        public ReliablePacket(Func<SequencedRawMessage> rawMessageActivator) : base(rawMessageActivator)
        {
        }

        public static int ChannelAckBufferLength { get; } = 128;

        public static int PacketAckBufferLength { get; } = 32;

        public SeqNo Seq { get; set; }

        public SeqNo Ack { get; set; }

        public BitVector AckBuffer { get; set; }

        protected override void DeserializeHeaderCore(INetDataReader reader)
        {
            Seq = reader.ReadSeqNo();
            Ack = reader.ReadSeqNo();
            AckBuffer = reader.ReadBits(PacketAckBufferLength);
            base.DeserializeHeaderCore(reader);
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
            base.SerializeHeaderCore(writer);
        }
    }
}
