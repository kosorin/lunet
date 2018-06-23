namespace Lure.Net.Packets.System
{
    internal enum PacketDataType : byte
    {
        ConnectionRequest = 0,
        ConnectionReject = 1,
        ConnectionChallenge = 2,
        ConnectionResponse = 3,
        Disconnect = 4,
        KeepAlive = 5,

        //// Data
        //PayloadUnreliable = 10,
        //PayloadUnreliableSequenced = 11,
        //PayloadReliable = 13,
        //PayloadReliableSequenced = 12,
        //PayloadReliableOrdered = 14,
    }
}
