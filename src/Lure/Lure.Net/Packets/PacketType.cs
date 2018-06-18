namespace Lure.Net.Packets
{
    internal enum PacketType : byte
    {
        ConnectRequest = 0,
        ConnectDeny = 1,
        ConnectChallenge = 1,
        ConnectResponse = 3,

        KeepAlive = 4,
        Payload = 5,
        Disconnect = 6,
    }
}
