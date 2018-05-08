using System.Net;

namespace Bur.Net.Tcp
{
    public class TcpEndPoint : IEndPoint
    {
        public TcpEndPoint(string hostName, int port)
        {
            Ip = IPAddress.Parse(hostName);
            Port = port;
        }

        public TcpEndPoint(IPAddress ipAddress, int port)
        {
            Ip = ipAddress;
            Port = port;
        }

        public IPAddress Ip { get; }

        public int Port { get; }

        public override string ToString()
        {
            return $"{Ip}:{Port}";
        }
    }
}
