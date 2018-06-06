using Serilog;
using System.Collections.Concurrent;
using System.Net;

namespace Lure.Net
{
    /// <summary>
    /// Represents a connection to a remote peer.
    /// </summary>
    public class NetConnection
    {
        private static readonly ILogger Logger = Log.ForContext<NetConnection>();

        private readonly NetPeer _peer;
        private readonly IPEndPoint _remoteEndPoint;
        private readonly ConcurrentQueue<NetMessage> _messageQueue;

        internal NetConnection(NetPeer peer, IPEndPoint remoteEndPoint)
        {
            _peer = peer;
            _remoteEndPoint = remoteEndPoint;
            _messageQueue = new ConcurrentQueue<NetMessage>();
        }

        public IPEndPoint RemoteEndPoint => _remoteEndPoint;

        internal NetPeer Peer => _peer;

        internal ConcurrentQueue<NetMessage> MessageQueue => _messageQueue;


        public void SendMessage(NetMessage message)
        {
            _messageQueue.Enqueue(message);
        }
    }
}
