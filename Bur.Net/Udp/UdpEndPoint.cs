using System.Net;

namespace Bur.Net.Udp
{
    public class UdpEndPoint : IEndPoint
    {
        public UdpEndPoint(string hostName, int port)
        {
            Ip = IPAddress.Parse(hostName);
            Port = port;
        }

        public UdpEndPoint(IPAddress ipAddress, int port)
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
