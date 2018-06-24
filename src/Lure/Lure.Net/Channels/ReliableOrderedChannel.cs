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
        private readonly SequencedRawMessageTracker _outgoingRawMessageTracker = new SequencedRawMessageTracker();

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


        protected override bool AcceptIncomingPacket(ReliablePacket packet)
        {
            if (AcknowledgeIncomingPacket(packet.Seq))
            {
                AcknowledgeOutgoingPackets(packet.Ack, packet.AckBuffer);
                return true;
            }
            return false;
        }

        protected override bool AcceptIncomingRawMessage(ReliableRawMessage rawMessage)
        {
            return true;
        }

        protected override void OnIncomingPacket(ReliablePacket packet)
        {
            _requireAcknowledgement = packet.RawMessages.Count > 0;
        }

        protected override void OnIncomingRawMessage(ReliableRawMessage rawMessage)
        {
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

        protected override void PrepareOutgoingPacket(ReliablePacket packet)
        {
            packet.Seq = _outgoingPacketSeq++;
            packet.Ack = _incomingPacketAck;
            packet.AckBuffer = _incomingPacketAckBuffer.Clone(0, ReliablePacket.PacketAckBufferLength);
        }

        protected override void PrepareOutgoingRawMessage(ReliableRawMessage rawMessage)
        {
            rawMessage.Seq = _outgoingMessageSeq++;
        }

        protected override void OnOutgoingPacket(ReliablePacket packet)
        {
            _outgoingRawMessageTracker.Track(packet.Seq, packet.RawMessages);
        }

        protected override void OnOutgoingRawMessage(ReliableRawMessage rawMessage)
        {
            lock (_outgoingRawMessageQueue)
            {
                if (!_outgoingRawMessageQueue.TryAdd(rawMessage.Seq, rawMessage))
                {
                    throw new NetException("Raw message buffer overflow.");
                }
            }
        }


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
            var rawMessageSeqs = _outgoingRawMessageTracker.GetSeqs(ack);
            if (rawMessageSeqs != null)
            {
                lock (_outgoingRawMessageQueue)
                {
                    foreach (var rawMessageSeq in rawMessageSeqs)
                    {
                        if (_outgoingRawMessageQueue.Remove(rawMessageSeq, out var rawMessage))
                        {
                            _rawMessagePool.Return(rawMessage);
                        }
                    }
                }
            }
        }
    }
}
