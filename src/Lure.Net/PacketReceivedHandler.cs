using Lure.Net.Data;
using System.Net;

namespace Lure.Net
{
    public delegate void PacketReceivedHandler<in TEndPoint>(TEndPoint remoteEndPoint, byte channelId, NetDataReader reader)
        where TEndPoint : IEndPoint;
}
