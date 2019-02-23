using System;

namespace Lure.Net
{
    public interface IConnectionListener : IDisposable
    {
        event TypedEventHandler<IConnectionListener, IConnection> NewConnection;

        void Start();

        void Stop();
    }
}
