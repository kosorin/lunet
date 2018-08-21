using Lure.Collections;
using Lure.Net.Data;
using Lure.Net.Extensions;

namespace Lure.Net.Packets
{
    internal class UnreliableSequencedPacket : NetPacket<SequencedRawMessage>, IPoolable
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
            if (Direction == NetPacketDirection.Outgoing)
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
            base.DeserializeHeaderCore(reader);
        }

        protected override void SerializeHeaderCore(INetDataWriter writer)
        {
            writer.WriteSeqNo(Seq);
            base.SerializeHeaderCore(writer);
        }
    }
}
