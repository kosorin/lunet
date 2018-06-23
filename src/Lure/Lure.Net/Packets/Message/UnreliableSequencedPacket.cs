using Lure.Collections;
using Lure.Net.Data;
using Lure.Net.Extensions;

namespace Lure.Net.Packets.Message
{
    internal class UnreliableSequencedPacket : MessagePacket<UnreliableRawMessage>, IPoolable
    {
        public UnreliableSequencedPacket(ObjectPool<UnreliableRawMessage> rawMessagePool) : base(rawMessagePool)
        {
        }

        public SeqNo Seq { get; set; }

        void IPoolable.OnRent()
        {
        }

        void IPoolable.OnReturn()
        {
            foreach (var rawMessage in RawMessages)
            {
                _rawMessagePool.Return(rawMessage);
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
