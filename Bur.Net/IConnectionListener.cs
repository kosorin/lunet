using Bur.Common;

namespace Bur.Net
{
    public interface IConnectionListener
    {
        event TypedEventHandler<IConnectionListener, ChannelConnectedEventArgs> ChannelConnected;

        void Start();

        void Stop();
    }
}
