using Lure.Net.Data;
using Lure.Net.Extensions;
using System;

namespace Lure.Net.Channels.Message
{
    public class ReliableMessage : Message, IComparable<ReliableMessage>
    {
        public long? Timestamp { get; set; }

        public SeqNo Seq { get; set; }

        public override int Length => SeqNo.SizeOf + base.Length;

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

        int IComparable<ReliableMessage>.CompareTo(ReliableMessage other)
        {
            return Seq.CompareTo(other.Seq);
        }
    }
}
