namespace Lure.Net.Tcp
{
    public class TcpConnectionListener : ConnectionListener<InternetEndPoint, TcpConnection>
    {
        private readonly TcpListenerSocket _socket;

        public TcpConnectionListener(ServerConfiguration config, IChannelFactory channelFactory) : base(channelFactory)
        {
            Config = config;

            _socket = new TcpListenerSocket(Config);
            _socket.AcceptSocket += Socket_AcceptSocket;
        }


        protected ServerConfiguration Config { get; }


        public override void Start()
        {
            _socket.Listen();
        }

        public override void Stop()
        {
            _socket.Close();
        }


        private void Socket_AcceptSocket(TcpListenerSocket serverSocket, TcpSocket clientSocket)
        {
            var remoteEndPoint = clientSocket.RemoteEndPoint;
            var connection = new TcpConnection(clientSocket, ChannelFactory);

            OnNewConnection(connection);
        }


        private bool disposed;

        protected override void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    Stop();
                }
                disposed = true;
            }
            base.Dispose(disposing);
        }
    }
}
