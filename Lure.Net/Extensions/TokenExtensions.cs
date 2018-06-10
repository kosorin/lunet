using Lure.Net.Data;
using Lure.Net.Messages;
using System.Net.Sockets;

namespace Lure.Net.Extensions
{
    internal static class TokenExtensions
    {
        public static NetDataReader GetReader(this SocketAsyncEventArgs token)
        {
            return new NetDataReader(token.Buffer, token.Offset, token.BytesTransferred);
        }

        public static void SetWriter(this SocketAsyncEventArgs token, NetDataWriter writer)
        {
            token.SetBuffer(writer.Data, writer.Offset, writer.Length);
        }
    }
}
