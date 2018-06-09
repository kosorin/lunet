using Lure.Net.Data;
using Lure.Net.Extensions;
using Lure.Net.Messages;
using Lure.Net.Packets;
using Serilog;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;

namespace Lure.Net
{
    /// <summary>
    /// Represents a connection to a remote peer.
    /// </summary>
    public class NetConnection
    {
        private const int MTU = 1000;
        private const int ResendTimeout = 100;

        private static readonly ILogger Logger = Log.ForContext<NetConnection>();

        private readonly NetPeer _peer;
        private readonly IPEndPoint _remoteEndPoint;
        private readonly INetDataWriter _writer = new NetDataWriter(MTU);
        private readonly Dictionary<ushort, PayloadMessage> _sendQueue = new Dictionary<ushort, PayloadMessage>();

        private readonly SequenceNumber _sendPacketSequence = new SequenceNumber();
        private readonly SequenceNumber _sendMessageSequence = new SequenceNumber();

        internal NetConnection(NetPeer peer, IPEndPoint remoteEndPoint)
        {
            _peer = peer;
            _remoteEndPoint = remoteEndPoint;
        }

        public NetPeer Peer => _peer;

        public IPEndPoint RemoteEndPoint => _remoteEndPoint;


        public void SendMessage(NetMessage message)
        {
            var data = SerializeMessage(message);

            lock (_sendQueue)
            {
                var id = _sendMessageSequence.GetNext();
                if (!_sendQueue.TryAdd(id, new PayloadMessage(id, data)))
                {
                    throw new NetException("Buffer overflow.");
                }
            }
        }

        internal void PreparePacket(Packet packet)
        {
            packet.Sequence = _sendPacketSequence.GetNext();
        }

        internal IEnumerable<Payload> GetQueuedPayloads()
        {
            List<PayloadMessage> messages;
            lock (_sendQueue)
            {
                if (_sendQueue.Count == 0)
                {
                    yield break;
                }
                messages = _sendQueue.Values
                    .Where(x => x.LastSendTimestamp == null || Peer.CurrentTimestamp - x.LastSendTimestamp > ResendTimeout)
                    .OrderBy(x => x.LastSendTimestamp ?? long.MaxValue)
                    .ToList();
            }

            foreach (var message in messages)
            {
                message.LastSendTimestamp = Peer.CurrentTimestamp;
            }

            var payload = new Payload();
            foreach (var message in messages)
            {
                if (message.Data.Length > MTU)
                {
                    throw new NetException();
                }
                else if (payload.TotalLength + message.Data.Length > MTU)
                {
                    yield return payload;
                    payload = new Payload();
                }

                payload.Messages.Add(message);
            }

            if (payload.Messages.Count > 0)
            {
                yield return payload;
            }
        }

        private byte[] SerializeMessage(NetMessage message)
        {
            lock (_writer)
            {
                _writer.Reset();
                _writer.WriteSerializable(message);
                _writer.Flush();
                return _writer.GetBytes();
            }
        }
    }
}
