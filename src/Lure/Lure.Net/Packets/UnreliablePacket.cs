using Lure.Collections;
using Lure.Net.Data;

namespace Lure.Net.Packets
{
    internal class UnreliablePacket : MessagePacket<RawMessage>, IPoolable
    {
        public UnreliablePacket(ObjectPool<RawMessage> rawMessagePool) : base(rawMessagePool)
        {
        }

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
        }

        protected override void SerializeHeaderCore(INetDataWriter writer)
        {
        }
    }
}
