using Lure.Net.Data;

namespace Lure.Net.Messages
{
    internal enum SystemMessageType : byte
    {
        ConnectionRequest = 0,
        ConnectionChallenge = 1,
        ConnectionResponse = 2,
        ConnectionAccept = 3,
        ConnectionReject = 4,
        Disconnect = 5,
        KeepAlive = 6,
        Debug = 31,
    }
}
