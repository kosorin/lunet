namespace Bur.Net
{
    public enum NetPacketType : byte
    {
        None,
        Ping,
        Pong,
        ConnectRequest,
        ConnectAccept,
        ConnectDeny,
        KeepAlive,
        Disconnect,
    }
}
