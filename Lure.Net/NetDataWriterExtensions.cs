using System.Net.Sockets;

namespace Lure.Net
{
    public static class NetDataWriterExtensions
    {
        public static void SetTokenBuffer(this NetDataWriter writer, SocketAsyncEventArgs token)
        {
            token.SetBuffer(writer.Data, writer.Offset, writer.Length);
        }
    }
}
