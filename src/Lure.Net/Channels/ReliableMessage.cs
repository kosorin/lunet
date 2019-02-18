using Lure.Net.Data;
using Lure.Net.Extensions;
using System;

namespace Lure.Net.Channels
{
    public class ReliableMessage : Message, IComparable<ReliableMessage>
    {
        public long? Timestamp { get; set; }

        public SeqNo Seq { get; set; }

        public override int HeaderLength => SeqNo.SizeOf;

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

        /// <summary>
        /// Compare reliable message.
        /// </summary>
        /// <remarks>
        /// Used for sorting received packets.
        /// </remarks>
        int IComparable<ReliableMessage>.CompareTo(ReliableMessage other)
        {
            return Seq.CompareTo(other.Seq);
        }
    }
}
