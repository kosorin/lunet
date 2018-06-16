namespace Lure.Net.Messages
{
    internal enum SystemMessageType : ushort
    {
        Test = 0,

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
