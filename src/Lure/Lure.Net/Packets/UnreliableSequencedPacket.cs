using Lure.Net.Data;
using Lure.Net.Extensions;
using System;

namespace Lure.Net.Packets
{
    public class UnreliableSequencedPacket : NetPacket<RawMessage>
    {
        public UnreliableSequencedPacket(Func<RawMessage> rawMessageActivator) : base(rawMessageActivator)
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
