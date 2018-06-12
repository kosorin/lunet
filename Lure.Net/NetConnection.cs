using Lure.Net.Data;
using Lure.Net.Extensions;
using Lure.Net.Messages;
using Lure.Net.Packets;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Lure.Net
{
    /// <summary>
    /// Represents a connection to a remote peer.
    /// </summary>
    public sealed class NetConnection : IDisposable
    {
        private const int MTU = 1000;
        private const int ResendTimeout = 100;
        private const int KeepAliveTimeout = 1000;

        private static readonly ILogger Logger = Log.ForContext<NetConnection>();

        private readonly PacketManager _packetManager;
        private readonly ObjectPool<NetDataWriter> _writerPool;

        private readonly NetPeer _peer;
        private readonly IPEndPoint _remoteEndPoint;
        private readonly Dictionary<ushort, PayloadMessage> _sendQueue = new Dictionary<ushort, PayloadMessage>();
        private readonly Dictionary<ushort, Payload> _sentPayloads = new Dictionary<ushort, Payload>();

        private SequenceNumber _sendPacketSequence = new SequenceNumber();
        private SequenceNumber _sendMessageSequence = new SequenceNumber();

        private bool _sendAck = false;

        private SequenceNumber _receivePacketAck;
        private BitVector _receivePacketAcks = new BitVector(Packet.AckBitsLength);

        private long _lastSendTimestamp = 0;

        internal NetConnection(NetPeer peer, IPEndPoint remoteEndPoint)
        {
            _packetManager = new PacketManager();
            _writerPool = new ObjectPool<NetDataWriter>(16, () => new NetDataWriter(MTU));

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
                _sendMessageSequence = _sendMessageSequence.GetNext();

                var id = _sendMessageSequence.GetNext().Value;
                if (!_sendQueue.TryAdd(id, new PayloadMessage(id, data)))
                {
                    throw new NetException("Buffer overflow.");
                }
            }
        }

        public void Dispose()
        {
            _packetManager.Dispose();
        }

        internal void Update()
        {
            var payloads = GetQueuedPayloads();
            foreach (var payload in payloads)
            {
                _lastSendTimestamp = Peer.CurrentTimestamp;

                var packet = PreparePacket<PayloadPacket>();

                packet.Data = SerializePayload(payload);

                Peer.SendPacket(this, packet);

                _sentPayloads[packet.Sequence.Value] = payload;

                _packetManager.Release(packet);
            }

            if (payloads.Count == 0 && (_sendAck || (Peer.CurrentTimestamp - _lastSendTimestamp) > KeepAliveTimeout))
            {
                _sendAck = false;
                _lastSendTimestamp = Peer.CurrentTimestamp;

                var packet = PreparePacket<KeepAlivePacket>();

                Peer.SendPacket(this, packet);

                _packetManager.Release(packet);
            }
        }

        internal TPacket PreparePacket<TPacket>() where TPacket : Packet
        {
            _sendPacketSequence = _sendPacketSequence.GetNext();

            var packet = _packetManager.Create<TPacket>();
            packet.Sequence = _sendPacketSequence;
            packet.Ack = _receivePacketAck;
            packet.Acks = _receivePacketAcks;
            return packet;
        }

        internal void AckReceive(SequenceNumber sequence, bool replyAck)
        {
            _sendAck = replyAck;

            //Logger.Debug("  {Vec}", _receivePacketAcks);

            var diff = sequence.GetDifference(_receivePacketAck);
            if (diff > Packet.AckBitsLength)
            {
                _receivePacketAck = sequence;
                _receivePacketAcks.ClearAll();
                return;
            }
            else if (diff < -Packet.AckBitsLength)
            {
                return;
            }

            if (diff > 0)
            {
                _receivePacketAck = sequence;
                _receivePacketAcks.LeftShift(diff);
                _receivePacketAcks.Set(0);
            }

            //Logger.Debug("  {Vec}", _receivePacketAcks);
        }

        internal void AckSend(SequenceNumber ack, BitVector acks)
        {
            lock (_sendQueue)
            {
                var i = ack.Value;
                AckSend(i);

                foreach (var bit in acks.AsBits())
                {
                    --i;
                    if (bit)
                    {
                        AckSend(i);
                    }
                }
            }
        }

        private void AckSend(ushort ack)
        {
            if (_sentPayloads.Remove(ack, out var payload))
            {
                foreach (var message in payload.Messages)
                {
                    _sendQueue.Remove(message.Id);
                }
            }
        }

        internal List<Payload> GetQueuedPayloads()
        {
            var payloads = new List<Payload>();

            List<PayloadMessage> payloadMessages;
            lock (_sendQueue)
            {
                if (_sendQueue.Count == 0)
                {
                    return payloads;
                }
                payloadMessages = _sendQueue.Values
                    .Where(x => x.LastSendTimestamp == null || Peer.CurrentTimestamp - x.LastSendTimestamp > ResendTimeout)
                    .OrderBy(x => x.LastSendTimestamp ?? long.MaxValue)
                    .ToList();
            }

            foreach (var payloadMessage in payloadMessages)
            {
                payloadMessage.LastSendTimestamp = Peer.CurrentTimestamp;
            }

            var payload = new Payload();
            foreach (var payloadMessage in payloadMessages)
            {
                if (payloadMessage.Data.Length > MTU)
                {
                    throw new NetException();
                }
                else if (payload.TotalLength + payloadMessage.Data.Length > MTU)
                {
                    payloads.Add(payload);
                    payload = new Payload();
                }

                payload.Messages.Add(payloadMessage);
            }

            if (payload.Messages.Count > 0)
            {
                payloads.Add(payload);
            }

            return payloads;
        }

        private byte[] SerializeMessage(NetMessage message)
        {
            var writer = _writerPool.Rent();
            try
            {
                writer.Reset();
                writer.WriteSerializable(message);
                writer.Flush();

                return writer.GetBytes();
            }
            finally
            {
                _writerPool.Return(writer);
            }
        }

        private byte[] SerializePayload(Payload payload)
        {
            var writer = _writerPool.Rent();
            try
            {
                writer.Reset();
                foreach (var payloadMessage in payload.Messages)
                {
                    writer.WriteUShort(payloadMessage.Id);
                    writer.WriteBytes(payloadMessage.Data);
                    writer.PadBits();
                }
                writer.Flush();

                return writer.GetBytes();
            }
            finally
            {
                _writerPool.Return(writer);
            }
        }
    }
}
