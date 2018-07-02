using Lure.Net.Data;
using Lure.Net.Extensions;
using System;

namespace Lure.Net.Packets
{
    internal class ReliableRawMessage : RawMessage, ISequencedRawMessage, IComparable<ReliableRawMessage>
    {
        public SeqNo Seq { get; set; }

        public int ReferenceCount { get; set; }

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

        int IComparable<ReliableRawMessage>.CompareTo(ReliableRawMessage other)
        {
            return Seq.CompareTo(other.Seq);
        }
    }
}
