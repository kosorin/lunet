using Lure.Net.Data;
using System.Diagnostics;

namespace Lure.Net.Packets
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    internal abstract class PacketData : IPacketPart
    {
        public abstract int Length { get; }

        public abstract void Deserialize(INetDataReader reader);

        public abstract void Serialize(INetDataWriter writer);
    }
}
