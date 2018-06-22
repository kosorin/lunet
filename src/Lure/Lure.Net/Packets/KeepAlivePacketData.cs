using Lure.Net.Data;

namespace Lure.Net.Packets
{
    [PacketData(PacketDataType.KeepAlive)]
    internal class KeepAlivePacketData : PacketData
    {
        public override int Length => 0;

        public override void Deserialize(INetDataReader reader)
        {
        }

        public override void Serialize(INetDataWriter writer)
        {
        }
    }
}
