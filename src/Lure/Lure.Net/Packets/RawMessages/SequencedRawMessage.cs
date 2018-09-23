using Lure.Net.Data;
using Lure.Net.Extensions;
using System;

namespace Lure.Net.Packets
{
    public class SequencedRawMessage : RawMessage, IComparable<SequencedRawMessage>
    {
        public SeqNo Seq { get; set; }

        public override int Length => sizeof(ushort) + base.Length;

        public override void Deserialize(NetDataReader reader)
        {
            Seq = reader.ReadSeqNo();
            base.Deserialize(reader);
        }

        public override void Serialize(NetDataWriter writer)
        {
            writer.WriteSeqNo(Seq);
            base.Serialize(writer);
        }

        int IComparable<SequencedRawMessage>.CompareTo(SequencedRawMessage other)
        {
            return Seq.CompareTo(other.Seq);
        }
    }
}
