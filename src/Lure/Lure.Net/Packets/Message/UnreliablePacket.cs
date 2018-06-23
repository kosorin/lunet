using Lure.Collections;
using Lure.Net.Data;

namespace Lure.Net.Packets.Message
{
    internal class UnreliablePacket : MessagePacket<UnreliableRawMessage>
    {
        public UnreliablePacket(ObjectPool<UnreliableRawMessage> rawMessagePool) : base(rawMessagePool)
        {
        }

        protected override void DeserializeHeaderCore(INetDataReader reader)
        {
        }

        protected override void SerializeHeaderCore(INetDataWriter writer)
        {
        }
    }
}
