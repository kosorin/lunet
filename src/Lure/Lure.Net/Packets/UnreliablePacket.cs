using Lure.Collections;
using Lure.Net.Data;

namespace Lure.Net.Packets
{
    internal class UnreliablePacket : NetPacket<RawMessage>, IPoolable
    {
        public UnreliablePacket(IObjectPool<RawMessage> rawMessagePool) : base(rawMessagePool)
        {
        }

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
    }
}
