using Lure.Collections;
using Lure.Extensions.NetCore;
using Lure.Net.Data;
using Lure.Net.Packets;
using Serilog;
using System.Collections.Generic;
using System.Linq;

namespace Lure.Net.Channels
{
    internal class ReliableOrderedChannel : PayloadChannel<ReliablePacket, ReliablePayloadPacketData>
    {
        private const int ResendTimeout = 100;

        private readonly Dictionary<SeqNo, byte[]> _outgoingMessageQueue = new Dictionary<SeqNo, byte[]>();
        private readonly Dictionary<SeqNo, ReliablePayloadPacketData> _outgoingPacketBuffer = new Dictionary<SeqNo, ReliablePayloadPacketData>();

        private SeqNo _outgoingMessageSeq = SeqNo.Zero;
        private SeqNo _outgoingPacketSeq = SeqNo.Zero;
        private SeqNo _incomingPacketAck = SeqNo.Zero - 1;
        private BitVector _incomingPacketAckBuffer = new BitVector(ReliablePacket.AckBufferLength);
        private bool _requireAcknowledgement;

        private bool _disposed;

        public ReliableOrderedChannel(byte id, NetConnection connection)
            : base(id, connection, PacketDataType.PayloadReliableOrdered)
        {
        }

        public override void Update()
        {
            base.Update();

            if (_requireAcknowledgement)
            {
                _requireAcknowledgement = false;

                var data = _dataPool.Rent();
                var packet = CreateOutgoingPacket(data);
                SendPacket(packet);
            }
        }

        public override void SendRawMessage(byte[] rawMessage)
        {
            lock (_outgoingMessageQueue)
            {
                if (!_outgoingMessageQueue.TryAdd(_outgoingMessageSeq++, rawMessage))
                {
                    throw new NetException("Buffer overflow.");
                }
            }
        }

        protected override bool AcceptIncomingPacket(ReliablePacket packet)
        {
            if (packet.DataType != PacketDataType.PayloadReliableOrdered)
            {
                return false;
            }

            if (AcknowledgeIncomingPacket(packet.Seq))
            {
                _requireAcknowledgement = true;
                AcknowledgeOutgoingPackets(packet.Ack, packet.AckBuffer);
                return true;
            }
            return false;
        }

        protected override void PrepareOutgoingPacket(ReliablePacket packet)
        {
            packet.DataType = PacketDataType.PayloadReliableOrdered;
            packet.Seq = _outgoingPacketSeq++;
            packet.Ack = _incomingPacketAck;
            packet.AckBuffer = _incomingPacketAckBuffer.Clone(0, ReliablePacket.PacketAckBufferLength);
        }

        protected override List<ReliablePayloadPacketData> CollectOutgoingData()
        {
            var dataList = new List<ReliablePayloadPacketData>();

            List<ReliablePayloadPacketData> payloadMessages;
            lock (_outgoingMessageQueue)
            {
                if (_outgoingMessageQueue.Count == 0)
                {
                    return dataList;
                }
                payloadMessages = _outgoingMessageQueue.Values
                    .Where(x => x.LastSendTimestamp == null || Timestamp.Current - x.LastSendTimestamp > ResendTimeout)
                    .OrderBy(x => x.LastSendTimestamp ?? long.MaxValue)
                    .ToList();
            }

            foreach (var payloadMessage in payloadMessages)
            {
                payloadMessage.LastSendTimestamp = Timestamp.Current;
            }

            var data = _packetDataPool.Rent<UnreliablePayloadPacketData>();
            foreach (var payloadMessage in payloadMessages)
            {
                if (payloadMessage.Length > MTU)
                {
                    throw new NetException("Message too big.");
                }
                else if (data.Length + payloadMessage.Length > MTU)
                {
                    dataList.Add(data);
                    data = _packetDataPool.Rent<UnreliablePayloadPacketData>();
                }

                data.RawMessages.Add(payloadMessage);
            }

            if (data.RawMessages.Count > 0)
            {
                dataList.Add(data);
            }

            return dataList;
        }

        protected override void ParseRawMessages(ReliablePacket packet)
        {
            throw new System.NotImplementedException();
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _packetPool.Dispose();
                }
                _disposed = true;
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// Acks received packet. Returns <c>true</c> if <paramref name="seq"/> wasn't acked yet.
        /// </summary>
        /// <param name="seq"></param>
        private bool AcknowledgeIncomingPacket(SeqNo seq)
        {
            var diff = seq.GetDifference(_incomingPacketAck);
            if (diff == 0)
            {
                return false;
            }
            else if (diff > 0)
            {
                _incomingPacketAck = seq;

                if (diff > _incomingPacketAckBuffer.Capacity)
                {
                    _incomingPacketAckBuffer.ClearAll();
                }
                else
                {
                    _incomingPacketAckBuffer.LeftShift(diff);
                    _incomingPacketAckBuffer.Set(diff - 1);
                }
                return true;
            }
            else
            {
                diff *= -1;
                if (diff <= _incomingPacketAckBuffer.Capacity)
                {
                    var ackIndex = diff - 1;
                    if (_incomingPacketAckBuffer[ackIndex])
                    {
                        return false;
                    }
                    else
                    {
                        _incomingPacketAckBuffer.Set(diff - 1);
                        return true;
                    }
                }
                return false;
            }
        }

        private void AcknowledgeOutgoingPackets(SeqNo ack, BitVector acks)
        {
            lock (_outgoingMessageQueue)
            {
                AcknowledgeOutgoingPacket(ack);
                foreach (var bit in acks.AsBits())
                {
                    ack--;
                    if (bit)
                    {
                        AcknowledgeOutgoingPacket(ack);
                    }
                }
            }
        }

        private void AcknowledgeOutgoingPacket(SeqNo ack)
        {
            if (_outgoingPacketBuffer.Remove(ack, out var data))
            {
                foreach (var message in data.RawMessages)
                {
                    _outgoingMessageQueue.Remove(message.Seq);
                }

                _packetDataPool.Return(data);
            }
        }
    }
}
