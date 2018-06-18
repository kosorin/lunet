using Lure.Net.Data;

namespace Lure.Net.Packets
{
    internal interface IPacketPart : INetSerializable
    {
        int Length { get; }
    }
}
