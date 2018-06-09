namespace Lure.Net.Packets
{
    internal enum PacketType : byte
    {
        Fragment,
        Payload,
        Ping,
        Pong,
        ConnectRequest,
        ConnectAccept,
        ConnectDeny,
        KeepAlive,
        Disconnect,
    }
}
