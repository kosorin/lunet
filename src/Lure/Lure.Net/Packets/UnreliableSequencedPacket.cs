using Lure.Collections;
using Lure.Net.Data;
using Lure.Net.Extensions;

namespace Lure.Net.Packets
{
    internal class UnreliableSequencedPacket : MessagePacket<SequencedRawMessage>, IPoolable
    {
        public UnreliableSequencedPacket(ObjectPool<SequencedRawMessage> rawMessagePool) : base(rawMessagePool)
        {
        }

        public SeqNo Seq { get; set; }

        void IPoolable.OnRent()
        {
        }

        void IPoolable.OnReturn()
        {
            if (Direction == PacketDirection.Outgoing)
            {
                foreach (var rawMessage in RawMessages)
                {
                    _rawMessagePool.Return(rawMessage);
                }
            }
            RawMessages.Clear();
        }

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
