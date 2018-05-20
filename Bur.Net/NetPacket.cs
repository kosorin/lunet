namespace Bur.Net
{
    public abstract class NetPacket
    {
        public NetPacketType Type { get; set; }

        public ushort Sequence { get; set; }

        public ushort Ack { get; set; }

        public uint AckBits { get; set; }
    }
}
