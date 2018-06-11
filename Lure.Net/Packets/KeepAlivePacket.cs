using System.Collections.Generic;
using Lure.Net.Data;
using Lure.Net.Extensions;
using Lure.Net.Messages;

namespace Lure.Net.Packets
{
    [Packet(PacketType.KeepAlive)]
    internal class KeepAlivePacket : Packet
    {
        public override PacketType Type => PacketType.KeepAlive;
    }
}
