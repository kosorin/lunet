namespace Lure.Net
{
    public abstract class ConnectionListener<TEndPoint, TConnection> : IConnectionListener<TEndPoint, TConnection>
        where TEndPoint : IEndPoint
        where TConnection : IConnection<TEndPoint>
    {
        protected ConnectionListener(IChannelFactory channelFactory)
        {
            ChannelFactory = channelFactory;
        }


        protected IChannelFactory ChannelFactory { get; }


        public event TypedEventHandler<IConnectionListener<TEndPoint, TConnection>, IConnection<TEndPoint>> NewConnection;

        event TypedEventHandler<IConnectionListener, IConnection> IConnectionListener.NewConnection
        {
            add => NewConnection += value;
            remove => NewConnection -= value;
        }

        protected virtual void OnNewConnection(TConnection connection)
        {
            NewConnection?.Invoke(this, connection);
        }


        public abstract void Start();

        public abstract void Stop();


        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
        }
    }
}
