using Lure.Net.Data;
using System.Net;

namespace Lure.Net.Packets
{
    internal class ReceivedPacketEventArgs
    {
        public ReceivedPacketEventArgs(IPEndPoint remoteEndPoint, INetDataReader reader)
        {
            RemoteEndPoint = remoteEndPoint;
            Reader = reader;
        }

        public IPEndPoint RemoteEndPoint { get; }

        public INetDataReader Reader { get; }
    }
}
