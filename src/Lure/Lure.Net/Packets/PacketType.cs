namespace Lure.Net.Packets
{
    internal enum PacketType : byte
    {
        System = 0,
        Fragment = 1,
        Reserved = 2,
        User = 3,
    }
}
