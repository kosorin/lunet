using Bur.Net.Tcp;

namespace Bur.Net.Server.Tcp
{
    public class TcpServer : NetServer
    {
        private readonly TcpEndPoint endPoint;

        public TcpServer(TcpEndPoint endPoint)
        {
            this.endPoint = endPoint;
        }

        protected override IConnectionListener CreateConnectionListener()
        {
            return new TcpConnectionListener(endPoint);
        }
    }
}
