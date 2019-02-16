using System.Net.Sockets;

namespace Lure.Net.Extensions
{
    internal static class TokenExtensions
    {
        public static bool IsOk(this SocketAsyncEventArgs token)
        {
            return token.SocketError == SocketError.Success && token.BytesTransferred > 0;
        }
    }
}
