using System.Net;

namespace Lunet.Extensions
{
    public static class EndPointExtensions
    {
        public static UdpEndPoint ToUdpEndPoint(this EndPoint endPoint)
        {
            return new UdpEndPoint(endPoint);
        }

        public static UdpEndPoint ToUdpEndPoint(this IPEndPoint endPoint)
        {
            return new UdpEndPoint(endPoint);
        }
    }
}
