using Lure.Net.Data;
using Lure.Net.Extensions;

namespace Lure.Net.Packets
{
    internal class SequencedPacket : Packet
    {
        public SeqNo Seq { get; set; }

        protected override void DeserializeHeaderCore(INetDataReader reader)
        {
            Seq = reader.ReadSeqNo();
        }

        protected override void SerializeHeaderCore(INetDataWriter writer)
        {
            writer.WriteSeqNo(Seq);
        }
    }
}
