using Lure.Net.Data;
using Lure.Net.Extensions;
using System;

namespace Lure.Net.Channels.Message
{
    public class UnreliableSequencedPacket : MessagePacket<Message>
    {
        public UnreliableSequencedPacket(Func<Message> messageActivator) : base(messageActivator)
        {
        }

        public SeqNo Seq { get; set; }

        protected override void DeserializeHeaderCore(NetDataReader reader)
        {
            Seq = reader.ReadSeqNo();
            base.DeserializeHeaderCore(reader);
        }

        protected override void SerializeHeaderCore(NetDataWriter writer)
        {
            writer.WriteSeqNo(Seq);
            base.SerializeHeaderCore(writer);
        }
    }
}
