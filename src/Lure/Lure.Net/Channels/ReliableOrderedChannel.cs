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
using System.Text;

namespace Lure.Net.Channels
{
    internal class ReliableOrderedChannel : NetChannel
    {
        private const int MTU = 1000;

        private static readonly ILogger Logger = Log.ForContext<ReliableOrderedChannel>();

        private readonly PacketManager _packetManager;
        private readonly ObjectPool<NetDataWriter> _writerPool;

        private readonly Dictionary<SeqNo, PayloadMessage> _sendQueue = new Dictionary<SeqNo, PayloadMessage>();
        private readonly Dictionary<SeqNo, Payload> _sentPayloads = new Dictionary<SeqNo, Payload>();
        private SeqNo _sendMessageSeq = SeqNo.Zero;
        private SeqNo _sendPacketSeq = SeqNo.Zero;

        private SeqNo _receivePacketAck = SeqNo.Zero - 1;
        private BitVector _receivePacketAckBuffer = new BitVector(Packet.AckBufferLength);

        public ReliableOrderedChannel(NetConnection connection) : base(connection)
        {
            _packetManager = new PacketManager();
            _writerPool = new ObjectPool<NetDataWriter>(16, () => new NetDataWriter(MTU));
        }


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
            _writerPool.Dispose();
        }



        protected override void PrepareOutgoingPacket(Packet packet)
        {
            packet.Seq = _sendPacketSeq++;
            packet.Ack = _receivePacketAck;
            packet.AckBuffer = _receivePacketAckBuffer.Clone(0, Packet.PacketAckBufferLength);
        }

        internal TPacket PreparePacket<TPacket>() where TPacket : Packet
        {
            var packet = _packetManager.Create<TPacket>();
            packet.Seq = _sendPacketSeq++;
            packet.Ack = _receivePacketAck;
            packet.AckBuffer = _receivePacketAckBuffer.Clone(0, Packet.PacketAckBufferLength);
            return packet;
        }

        /// <summary>
        /// Acks received packet.
        /// </summary>
        /// <param name="seq"></param>
        /// <returns>Returns <c>true</c> if <paramref name="seq"/> wasn't acked yet.</returns>
        internal bool AckReceive(SeqNo seq)
        {
            var diff = seq.GetDifference(_receivePacketAck);
            if (diff == 0)
            {
                return false;
            }
            else if (diff > 0)
            {
                _receivePacketAck = seq;

                if (diff > _receivePacketAckBuffer.Capacity)
                {
                    _receivePacketAckBuffer.ClearAll();
                }
                else
                {
                    _receivePacketAckBuffer.LeftShift(diff);
                    _receivePacketAckBuffer.Set(diff - 1);
                }
                goto Success;
            }
            else
            {
                diff *= -1;
                if (diff <= _receivePacketAckBuffer.Capacity)
                {
                    var ackIndex = diff - 1;
                    if (_receivePacketAckBuffer[ackIndex])
                    {
                        return false;
                    }
                    else
                    {
                        _receivePacketAckBuffer.Set(diff - 1);
                        goto Success;
                    }
                }
                return false;
            }

            Success:
            Logger.Verbose("  {Acks} <- {Ack}", _receivePacketAckBuffer, _receivePacketAck.Value);
            return true;
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
