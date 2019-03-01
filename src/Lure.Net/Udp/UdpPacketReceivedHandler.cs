namespace Lunet.Udp
{
    internal delegate void UdpPacketReceivedHandler(InternetEndPoint remoteEndPoint, byte[] data, int offset, int length);
}
