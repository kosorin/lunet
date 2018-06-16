using Lure.Extensions.NetCore;
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

        private SeqNo _sendMessageSeq = SeqNo.Zero;
        private readonly Dictionary<SeqNo, PayloadMessage> _sendQueue = new Dictionary<SeqNo, PayloadMessage>();

        private long _lastPacketTimestamp = 0;
        private SeqNo _sendPacketSeq = SeqNo.Zero;
        private readonly Dictionary<SeqNo, Payload> _sentPayloads = new Dictionary<SeqNo, Payload>();

        private bool _sendAck = false;

        private SeqNo _receivePacketAck = SeqNo.Zero - 1;
        private BitVector _receivePacketAckBuffer = new BitVector(Packet.AcksLength);

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
                var messageSeq = _sendMessageSeq++;
                if (!_sendQueue.TryAdd(messageSeq, new PayloadMessage(messageSeq, data)))
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
                var packet = PreparePacket<PayloadPacket>();
                packet.Data = SerializePayload(payload);
                _sentPayloads[packet.Seq] = payload;
                SendPacket(packet);
            }

            if (payloads.Count == 0 && (_sendAck || (Timestamp.Current - _lastPacketTimestamp) > KeepAliveTimeout))
            {
                _sendAck = false;
                SendPacket(PreparePacket<KeepAlivePacket>());
            }
        }

        internal TPacket PreparePacket<TPacket>() where TPacket : Packet
        {
            var packet = _packetManager.Create<TPacket>();
            packet.Seq = _sendPacketSeq++;
            packet.Ack = _receivePacketAck;
            packet.AckBuffer = _receivePacketAckBuffer;
            return packet;
        }

        internal void AckReceive(SeqNo seq)
        {
            var diff = seq.GetDifference(_receivePacketAck);
            if (Math.Abs(diff) > Packet.AcksLength)
            {
                throw new NetException("Receive ack buffer is out of sync!");
            }

            if (diff > 0)
            {
                _receivePacketAck = seq;
                _receivePacketAckBuffer.LeftShift(diff);
                _receivePacketAckBuffer.Set(0);
            }
            else
            {

            }

            Logger.Verbose("  {Acks} <- {Ack}", _receivePacketAckBuffer, _receivePacketAck.Value);
        }

        internal void AckSend(SeqNo ack, BitVector acks)
        {
            lock (_sendQueue)
            {
                AckSend(ack);
                foreach (var bit in acks.AsBits())
                {
                    ack--;
                    if (bit)
                    {
                        AckSend(ack);
                    }
                }
            }
        }

        private void AckSend(SeqNo ack)
        {
            if (_sentPayloads.Remove(ack, out var payload))
            {
                foreach (var message in payload.Messages)
                {
                    _sendQueue.Remove(message.Seq);
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
                    .Where(x => x.LastSendTimestamp == null || Timestamp.Current - x.LastSendTimestamp > ResendTimeout)
                    .OrderBy(x => x.LastSendTimestamp ?? long.MaxValue)
                    .ToList();
            }

            foreach (var payloadMessage in payloadMessages)
            {
                payloadMessage.LastSendTimestamp = Timestamp.Current;
            }

            var payload = new Payload();
            foreach (var payloadMessage in payloadMessages)
            {
                if (payloadMessage.Length > MTU)
                {
                    throw new NetException();
                }
                else if (payload.Length + payloadMessage.Length > MTU)
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

        private void SendPacket(Packet packet)
        {
            Peer.SendPacket(this, packet);
            _packetManager.Return(packet);
            _lastPacketTimestamp = Timestamp.Current;
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
                    writer.WriteSeqNo(payloadMessage.Seq);
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
