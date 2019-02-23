using System;

namespace Lure.Net
{
    public interface IConnection : IDisposable
    {
        int MTU { get; }

        int RTT { get; }

        ConnectionState State { get; }

        IEndPoint RemoteEndPoint { get; }

        event TypedEventHandler<IConnection> Disconnected;

        event TypedEventHandler<IChannel, byte[]> MessageReceived;

        void Update();

        void Connect();

        void Disconnect();

        void SendMessage(byte[] data);

        void SendMessage(byte channelId, byte[] data);
    }

    public interface IConnection<TEndPoint> : IConnection
        where TEndPoint : IEndPoint
    {
        new TEndPoint RemoteEndPoint { get; }
    }
}
