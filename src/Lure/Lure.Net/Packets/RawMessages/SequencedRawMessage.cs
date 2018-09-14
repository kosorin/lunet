using Lure.Net.Data;
using Lure.Net.Extensions;
using System;

namespace Lure.Net.Packets
{
    public class SequencedRawMessage : RawMessage, IComparable<SequencedRawMessage>
    {
        public SeqNo Seq { get; set; }

        public override int Length => sizeof(ushort) + base.Length;

        protected override string DebuggerDisplay => $"({Seq}) {base.DebuggerDisplay}";

        public override void Deserialize(INetDataReader reader)
        {
            Seq = reader.ReadSeqNo();
            base.Deserialize(reader);
        }

        public override void Serialize(INetDataWriter writer)
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
