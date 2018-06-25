using Lure.Net.Data;

namespace Lure.Net.Channels
{
    internal interface INetChannel
    {
        byte Id { get; }
        long LastIncomingPacketTimestamp { get; }
        long LastOutgoingPacketTimestamp { get; }

        void Dispose();
        void ReceivePacket(NetDataReader reader);
        void Update();
    }
}