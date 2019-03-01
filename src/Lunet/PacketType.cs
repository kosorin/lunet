namespace Lunet.Packets
{
    internal enum PacketType : byte
    {
        User = 0,
        System = 1,
        Fragment = 2,

        // Max 16 fields (4 bits)
    }
}
