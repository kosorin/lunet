using Lure.Net.Data;
using Lure.Net.Extensions;
using System;

namespace Lure.Net.Channels
{
    public class ReliablePacket : MessagePacket<ReliableMessage>
    {
        public ReliablePacket(Func<ReliableMessage> messageActivator) : base(messageActivator)
        {
        }

        public static int AckBufferLength { get; } = 32;

        public SeqNo Seq { get; set; }

        public SeqNo Ack { get; set; }

        public BitVector AckBuffer { get; set; }

        public override int HeaderLength => SeqNo.SizeOf + SeqNo.SizeOf + AckBufferLength;

        protected override void DeserializeHeaderCore(NetDataReader reader)
        {
            Seq = reader.ReadSeqNo();
            Ack = reader.ReadSeqNo();
            AckBuffer = reader.ReadBits(AckBufferLength);
            base.DeserializeHeaderCore(reader);
        }

        protected override void DeserializeDataCore(NetDataReader reader)
        {
            base.DeserializeDataCore(reader);
            Messages.Sort();
        }

        protected override void SerializeHeaderCore(NetDataWriter writer)
        {
            writer.WriteSeqNo(Seq);
            writer.WriteSeqNo(Ack);
            writer.WriteBits(AckBuffer);
            base.SerializeHeaderCore(writer);
        }
    }
}
