namespace Lure.Net
{
    public class SocketStatistics
    {
        public ulong ReceivedBytes { get; internal set; }

        public ulong SentBytes { get; internal set; }


        public int ReceivedPackets { get; internal set; }

        public int SentPackets { get; internal set; }
    }
}
