using Lure.Net.Data;
using System.Net;

namespace Lure.Net
{
    internal delegate void PacketReceivedHandler(IPEndPoint remoteEndPoint, byte channelId, NetDataReader reader);
}
