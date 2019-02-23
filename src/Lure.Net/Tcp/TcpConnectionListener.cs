namespace Lure.Net.Tcp
{
    public class TcpConnectionListener : ConnectionListener
    {
        private readonly TcpListenerSocket _socket;

        public TcpConnectionListener(InternetEndPoint localEndPoint, IChannelFactory channelFactory) : base(channelFactory)
        {
            _socket = new TcpListenerSocket(localEndPoint);
            _socket.AcceptSocket += Socket_AcceptSocket;
        }


        public override void Start()
        {
            _socket.Listen();
        }

        public override void Stop()
        {
            _socket.Close();
        }


        private void Socket_AcceptSocket(TcpListenerSocket listenerSocket, TcpSocket socket)
        {
            var remoteEndPoint = socket.RemoteEndPoint;
            var connection = new TcpServerConnection(socket, ChannelFactory);

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
