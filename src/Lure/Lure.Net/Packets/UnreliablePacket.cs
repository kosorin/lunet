using Lure.Net.Data;

namespace Lure.Net.Packets
{
    internal class UnreliablePacket : Packet
    {
        protected override void DeserializeHeaderCore(INetDataReader reader)
        {
        }

        protected override void SerializeHeaderCore(INetDataWriter writer)
        {
        }
    }
}
