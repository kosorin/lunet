using Lure.Extensions.NetCore;
using Lure.Net.Data;
using Lure.Net.Messages;
using Lure.Net.Packets.Message;
using Serilog;
using System.Collections.Generic;
using System.Linq;

namespace Lure.Net.Channels
{
    internal class ReliableOrderedChannel : MessageChannel<ReliablePacket, ReliableRawMessage>
    {
        private const int ResendTimeout = 100;

        private readonly Dictionary<SeqNo, ReliableRawMessage> _outgoingRawMessageQueue = new Dictionary<SeqNo, ReliableRawMessage>();
        private readonly Dictionary<SeqNo, List<ReliableRawMessage>> _outgoingPacketBuffer = new Dictionary<SeqNo, List<ReliableRawMessage>>();

        private SeqNo _outgoingMessageSeq = SeqNo.Zero;
        private SeqNo _outgoingPacketSeq = SeqNo.Zero;

        private SeqNo _incomingPacketAck = SeqNo.Zero - 1;
        private BitVector _incomingPacketAckBuffer = new BitVector(ReliablePacket.AckBufferLength);

        private bool _requireAcknowledgement;

        public ReliableOrderedChannel(byte id, NetConnection connection) : base(id, connection)
        {
        }

        public override void Update()
        {
            base.Update();

            if (_requireAcknowledgement)
            {
                _requireAcknowledgement = false;

                SendPacket(CreateOutgoingPacket());
            }
        }

        public override void SendRawMessage(byte[] data)
        {
            lock (_outgoingRawMessageQueue)
            {
                var rawMessage = _rawMessagePool.Rent();
                rawMessage.Seq = _outgoingMessageSeq++;
                rawMessage.Data = data;

                if (!_outgoingRawMessageQueue.TryAdd(rawMessage.Seq, rawMessage))
                {
                    throw new NetException("Raw message buffer overflow.");
                }
            }
        }

        protected override bool AcceptIncomingPacket(ReliablePacket packet)
        {
            if (AcknowledgeIncomingPacket(packet.Seq))
            {
                AcknowledgeOutgoingPackets(packet.Ack, packet.AckBuffer);

                _requireAcknowledgement = true;
                return true;
            }
            return false;
        }

        protected override void PrepareOutgoingPacket(ReliablePacket packet)
        {
            packet.Seq = _outgoingPacketSeq++;
            packet.Ack = _incomingPacketAck;
            packet.AckBuffer = _incomingPacketAckBuffer.Clone(0, ReliablePacket.PacketAckBufferLength);
        }

        protected override void OnOutgoingPacket(ReliablePacket packet)
        {
            base.OnOutgoingPacket(packet);

            _outgoingPacketBuffer.Add(packet.Seq, packet.RawMessages);
        }

        protected override List<ReliableRawMessage> CollectOutgoingRawMessages()
        {
            var now = Timestamp.Current;
            lock (_outgoingRawMessageQueue)
            {
                if (_outgoingRawMessageQueue.Count == 0)
                {
                    return new List<ReliableRawMessage>();
                }
                return _outgoingRawMessageQueue.Values
                    .Where(x => x.Timestamp == null || now - x.Timestamp > ResendTimeout)
                    .OrderBy(x => x.Timestamp ?? long.MaxValue)
                    .ToList();
            }
        }

        protected override void ParseRawMessages(ReliablePacket packet)
        {
            foreach (var rawMessage in packet.RawMessages)
            {
                var reader = new NetDataReader(rawMessage.Data);
                var typeId = reader.ReadUShort();
                var message = NetMessageManager.Create(typeId);
                message.Deserialize(reader);

                Log.Information("  {Message}", message);
            }
        }

        /// <summary>
        /// Acks received packet. Returns <c>true</c> for new packets.
        /// </summary>
        /// <param name="seq"></param>
        private bool AcknowledgeIncomingPacket(SeqNo seq)
        {
            var diff = seq.CompareTo(_incomingPacketAck);
            if (diff == 0)
            {
                // Drop already received packet
                return false;
            }
            else if (diff > 0)
            {
                _incomingPacketAck = seq;

                if (diff > _incomingPacketAckBuffer.Capacity)
                {
                    // Early packet
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
                if (diff > _incomingPacketAckBuffer.Capacity)
                {
                    // Drop late packet
                    return false;
                }
                else
                {
                    var ackIndex = diff - 1;
                    if (_incomingPacketAckBuffer[ackIndex])
                    {
                        // Drop already received packet
                        return false;
                    }
                    else
                    {
                        _incomingPacketAckBuffer.Set(diff - 1);
                        return true;
                    }
                }
            }
        }

        private void AcknowledgeOutgoingPackets(SeqNo ack, BitVector acks)
        {
            lock (_outgoingRawMessageQueue)
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
            lock (_outgoingRawMessageQueue)
            {
                if (_outgoingPacketBuffer.Remove(ack, out var rawMessages))
                {
                    foreach (var rawMessage in rawMessages)
                    {
                        _outgoingRawMessageQueue.Remove(rawMessage.Seq);
                        _rawMessagePool.Return(rawMessage);
                    }
                }
            }
        }
    }
}
