using Lure.Net.Data;

namespace Lure.Net.Packets
{
    internal class KeepAlivePacketData : PacketData
    {
        public override int Length => 0;

        public override void Deserialize(INetDataReader reader)
        {
            // Empty
        }

        public override void Serialize(INetDataWriter writer)
        {
            // Empty
        }
    }
}
