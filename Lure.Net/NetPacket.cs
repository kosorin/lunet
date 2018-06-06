using System.Collections.Generic;

namespace Lure.Net
{
    public class NetPacket
    {
        public NetPacketType Type { get; set; }

        public ushort Sequence { get; set; }

        public ushort Ack { get; set; }

        public uint AckBits { get; set; }

        public List<NetMessage> Messages { get; set; }
    }
}
