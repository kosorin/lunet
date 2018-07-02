using Lure.Net.Data;

namespace Lure.Net.Messages
{
    internal enum SystemMessageType : byte
    {
        ConnectionRequest = 0,
        ConnectionReject = 1,
        ConnectionChallenge = 2,
        ConnectionResponse = 3,
        Disconnect = 4,
        KeepAlive = 5,

        Debug = 31,
    }
}
