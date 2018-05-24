using Serilog;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Lure.Net
{
    /// <summary>
    /// Represents a connection to remote peer.
    /// </summary>
    internal class NetConnection
    {
        private static readonly ILogger Logger = Log.ForContext<NetConnection>();

        private readonly Socket _socket;
        private readonly IPEndPoint _remoteEndPoint;

        public NetConnection(Socket socket, IPEndPoint remoteEndPoint)
        {
            _socket = socket;
            _remoteEndPoint = remoteEndPoint;
        }


        public IPEndPoint RemoteEndPoint => _remoteEndPoint;


        public void SendMessage(string message)
        {
            var sendBuffer = Encoding.UTF8.GetBytes(message);
            _socket.BeginSendTo(sendBuffer, 0, sendBuffer.Length, SocketFlags.None, RemoteEndPoint, SendCallback, null);
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                var size = _socket.EndSendTo(ar);
                Logger.Verbose("[{RemoteEndPoint}] Sent data (size={Size})", RemoteEndPoint, size);
            }
            catch (SocketException e)
            {
                Logger.Error(e, "[{RemoteEndPoint}] Unable to send data ({SocketErrorCode}={ErrorCode})", RemoteEndPoint, e.SocketErrorCode, e.ErrorCode);
            }
            catch (Exception e)
            {
                Logger.Error(e, "[{RemoteEndPoint}] Unable to send data", RemoteEndPoint);
            }
        }
    }
}
