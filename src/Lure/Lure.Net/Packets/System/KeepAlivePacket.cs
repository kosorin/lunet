using Lure.Net.Data;

namespace Lure.Net.Packets.System
{
    [SystemPacket(SystemPacketType.KeepAlive)]
    internal class KeepAlivePacket : SystemPacket
    {
        protected override void DeserializeDataCore(INetDataReader reader)
        {
        }

        protected override void SerializeDataCore(INetDataWriter writer)
        {
        }
    }
}
