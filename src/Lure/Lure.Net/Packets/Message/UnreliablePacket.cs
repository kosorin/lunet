using Lure.Collections;
using Lure.Net.Data;

namespace Lure.Net.Packets.Message
{
    internal class UnreliablePacket : MessagePacket<UnreliableRawMessage>, IPoolable
    {
        public UnreliablePacket(ObjectPool<UnreliableRawMessage> rawMessagePool) : base(rawMessagePool)
        {
        }

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
        }

        protected override void SerializeHeaderCore(INetDataWriter writer)
        {
        }
    }
}
