namespace Lure.Net
{
    public abstract class ConnectionListener : IConnectionListener
    {
        protected ConnectionListener(IChannelFactory channelFactory)
        {
            ChannelFactory = channelFactory;
        }


        protected IChannelFactory ChannelFactory { get; }


        public event TypedEventHandler<IConnectionListener, IConnection> NewConnection;

        protected virtual void OnNewConnection(IConnection connection)
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
