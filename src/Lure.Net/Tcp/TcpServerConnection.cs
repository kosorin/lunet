using System;

namespace Lure.Net.Tcp
{
    internal class TcpServerConnection : TcpConnection
    {
        internal TcpServerConnection(TcpSocket socket, IChannelFactory channelFactory) : base(socket, channelFactory)
        {
            State = ConnectionState.Connected;
        }

        public override void Connect()
        {
            throw new InvalidOperationException($"{nameof(TcpConnection)} is automatically connected by listener.");
        }
    }
}
