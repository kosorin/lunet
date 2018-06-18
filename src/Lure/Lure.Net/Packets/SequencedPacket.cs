using Lure.Net.Data;
using Lure.Net.Extensions;

namespace Lure.Net.Packets
{
    internal class SequencedPacket : Packet
    {
        public SeqNo Seq { get; set; }

        protected override void DeserializeCore(INetDataReader reader)
        {
            Seq = reader.ReadSeqNo();
        }

        protected override void SerializeCore(INetDataWriter writer)
        {
            writer.WriteSeqNo(Seq);
        }
    }
}
