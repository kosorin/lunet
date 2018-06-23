using Lure.Collections;
using Lure.Net.Data;
using Lure.Net.Extensions;

namespace Lure.Net.Packets.Message
{
    internal class UnreliableSequencedPacket : MessagePacket<UnreliableRawMessage>
    {
        public UnreliableSequencedPacket(ObjectPool<UnreliableRawMessage> rawMessagePool) : base(rawMessagePool)
        {
        }

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
