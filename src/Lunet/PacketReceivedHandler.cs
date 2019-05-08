namespace Lunet
{
    internal delegate void PacketReceivedHandler(InternetEndPoint remoteEndPoint, byte[] data, int offset, int length);
}
