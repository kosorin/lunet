namespace Lure.Net.Udp
{
    public abstract class UdpConnection : Connection<InternetEndPoint>
    {
        protected UdpConnection(InternetEndPoint remoteEndPoint, IChannelFactory channelFactory) : base(remoteEndPoint, channelFactory)
        {
        }
    }
}
