using Lure.Net.Data;

namespace Lure.Net.Tcp
{
    public abstract class TcpConnection : Connection<InternetEndPoint>
    {
        private readonly TcpSocket _socket;
        private readonly TcpStreamParser _streamParser = new TcpStreamParser();

        internal TcpConnection(TcpSocket socket, IChannelFactory channelFactory) : base(socket.RemoteEndPoint, channelFactory)
        {
            _socket = socket;
            _socket.Disconnected += Socket_Disconnected;
            _socket.DataReceived += Socket_DataReceived;
        }


        public override void Connect()
        {
            State = ConnectionState.Connecting;
            _socket.Connect();

            State = ConnectionState.Connected;
        }

        public override void Disconnect()
        {
            State = ConnectionState.Disconnecting;
            _socket.Close();

            State = ConnectionState.Disconnected;
            OnDisconnected();
        }


        private void Socket_Disconnected(TcpSocket socket)
        {
            State = ConnectionState.Disconnected;
            OnDisconnected();
        }

        private void Socket_DataReceived(NetDataReader reader)
        {
            try
            {
                while (reader.Position < reader.Length)
                {
                    if (_streamParser.Next(reader))
                    {
                        var buffer = _streamParser.Buffer;
                        HandleReceivedPacket(buffer.Data, buffer.Offset, buffer.Length);
                    }
                }
            }
            catch
            {
                // TODO: Bad TCP data
                Disconnect();
            }
        }

        internal override void HandleSendPacket(ProtocolPacket packet)
        {
            _socket.SendPacket(packet.ChannelId, packet.ChannelPacket);
        }


        private bool _disposed;

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    Disconnect();
                }
                _disposed = true;
            }
            base.Dispose(disposing);
        }
    }
}
