using System;

namespace Lunet
{
    public class ProtocolPacket
    {
        private static Guid Version { get; } = Guid.Parse("1EDEFE8C-9469-4D68-9F3E-40A4A1971B90");

        static ProtocolPacket()
        {
        }


        public byte ChannelId { get; set; }

        public IChannelPacket ChannelPacket { get; set; }
    }
}
