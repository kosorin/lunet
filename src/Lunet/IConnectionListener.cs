using Lunet.Common;
using System;

namespace Lunet
{
    public interface IConnectionListener : IDisposable
    {
        event TypedEventHandler<IConnectionListener, IConnection> NewConnection;

        void Start();

        void Stop();
    }
}
