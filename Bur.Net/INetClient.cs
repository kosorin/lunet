using Bur.Common;

namespace Bur.Net.Server
{
    public interface INetClient
    {
        long Id { get; }

        ConnectionState State { get; }

        IEndPoint RemoteEndPoint { get; }

        event TypedEventHandler<INetClient, DataReceivedEventArgs> DataReceived;

        event TypedEventHandler<INetClient> Disconnected;

        void Disconnect();
    }
}
