using Bur.Common;

namespace Bur.Net
{
    public interface IChannel
    {
        IEndPoint RemoteEndPoint { get; }

        event TypedEventHandler<IChannel, DataReceivedEventArgs> DataReceived;

        event TypedEventHandler<IChannel> Stopped;

        void Start();

        void Stop();

        ConnectionState GetCurrentState();
    }
}
