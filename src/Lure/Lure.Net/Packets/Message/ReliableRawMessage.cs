using Lure.Net.Data;
using Lure.Net.Extensions;

namespace Lure.Net.Packets.Message
{
    internal class ReliableRawMessage : RawMessage
    {
        public SeqNo Seq { get; set; }

        public override int Length => sizeof(ushort) + base.Length;

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
    }
}
