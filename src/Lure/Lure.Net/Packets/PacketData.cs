using Lure.Net.Data;

namespace Lure.Net.Packets
{
    internal abstract class PacketData : IPacketPart
    {
        public abstract int Length { get; }

        public abstract void Deserialize(INetDataReader reader);

        public abstract void Serialize(INetDataWriter writer);
    }
}
