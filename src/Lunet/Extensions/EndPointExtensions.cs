using System.Net;

namespace Lunet.Extensions
{
    public static class EndPointExtensions
    {
        public static UdpEndPoint ToUdpEndPoint(this EndPoint endPoint)
        {
            // TODO: new
            return new UdpEndPoint(endPoint);
        }

        public static UdpEndPoint ToUdpEndPoint(this IPEndPoint endPoint)
        {
            // TODO: new
            return new UdpEndPoint(endPoint);
        }
    }
}
