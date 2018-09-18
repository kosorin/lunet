namespace Lure.Net
{
    public class NetPeerStatistics
    {
        public ulong ReceivedBytes { get; internal set; }

        public ulong SentBytes { get; internal set; }


        public int ReceivedPackets { get; internal set; }

        public int DroppedPackets { get; internal set; }

        public int SentPackets { get; internal set; }
    }
}
