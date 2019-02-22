namespace Lure.Net.Udp
{
    public abstract class UdpConnection : Connection<InternetEndPoint>
    {
        internal UdpConnection(InternetEndPoint remoteEndPoint, IChannelFactory channelFactory) : base(remoteEndPoint, channelFactory)
        {
        }
    }
}
