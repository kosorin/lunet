namespace Lure.Net.Tcp
{
    public class TcpClientConnection : TcpConnection
    {
        public TcpClientConnection(InternetEndPoint remoteEndPoint, IChannelFactory channelFactory) : base(new TcpSocket(remoteEndPoint), channelFactory)
        {
            State = ConnectionState.Disconnected;
        }
    }
}
