using Lure.Net.Packets;

namespace Lure.Net
{
    internal interface IPacketReceiver
    {
        event TypedEventHandler<IPacketReceiver, ReceivedPacketEventArgs> Received;
    }
}
