using Lure.Net.Data;
using Lure.Net.Extensions;
using System;

namespace Lure.Net.Packets
{
    public class UnreliableSequencedPacket : NetPacket<SequencedRawMessage>
    {
        public UnreliableSequencedPacket(Func<SequencedRawMessage> rawMessageActivator) : base(rawMessageActivator)
        {
        }

        public SeqNo Seq { get; set; }

        protected override void DeserializeHeaderCore(INetDataReader reader)
        {
            Seq = reader.ReadSeqNo();
            base.DeserializeHeaderCore(reader);
        }

        protected override void SerializeHeaderCore(INetDataWriter writer)
        {
            writer.WriteSeqNo(Seq);
            base.SerializeHeaderCore(writer);
        }
    }
}
