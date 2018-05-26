namespace Lure.Net
{
    public class NetPacketHeader
    {
        public NetPacketType Type { get; set; }

        public ushort Sequence { get; set; }

        public ushort Ack { get; set; }

        public uint AckBits { get; set; }
    }
}
