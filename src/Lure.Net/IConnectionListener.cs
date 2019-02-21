using System;

namespace Lure.Net
{
    public interface IConnectionListener : IDisposable
    {
        event TypedEventHandler<IConnectionListener, IConnection> NewConnection;

        void Start();

        void Stop();
    }

    public interface IConnectionListener<TEndPoint, TConnection> : IConnectionListener
        where TEndPoint : IEndPoint
        where TConnection : IConnection<TEndPoint>
    {
        new event TypedEventHandler<IConnectionListener<TEndPoint, TConnection>, IConnection<TEndPoint>> NewConnection;
    }
}
