namespace Lunet
{
    internal enum PacketType : byte
    {
        Channel = 0,
        System = 1,
        Fragment = 2,

        // Max 8 fields (3 bits)
    }
}
