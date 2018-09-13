using Lure.Net.Packets;
using System.Net;

namespace Lure.Net
{
    internal interface IPacketSender
    {
        void Send(IPEndPoint remoteEndPoint, byte channelId, INetPacket packet);
    }
}
