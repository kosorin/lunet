namespace Lure.Net
{
    public enum NetPacketType : byte
    {
        Message,
        Ping,
        Pong,
        ConnectRequest,
        ConnectAccept,
        ConnectDeny,
        KeepAlive,
        Disconnect,
    }
}
