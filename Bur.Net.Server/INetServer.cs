using Bur.Common;

namespace Bur.Net.Server
{
    public interface INetServer
    {
        event TypedEventHandler<INetServer, NetClientConnectedEventArgs> ClientConnected;

        event TypedEventHandler<INetServer, NetClientDisconnectedEventArgs> ClientDisconnected;

        void Start();

        void Stop();
    }
}
