namespace Lure.Net.Packets
{
    internal enum PacketDataType : byte
    {
        ConnectRequest = 0,
        ConnectDeny = 1,
        ConnectChallenge = 2,
        ConnectResponse = 3,

        KeepAlive = 4,
        Payload = 5,
        Disconnect = 6,
    }
}
