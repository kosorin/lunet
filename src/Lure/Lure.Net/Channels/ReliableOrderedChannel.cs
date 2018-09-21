using Lure.Extensions.NetCore;
using Lure.Net.Data;
using Lure.Net.Packets;
using System.Collections.Generic;
using System.Linq;

namespace Lure.Net.Channels
{
    public class ReliableOrderedChannel : NetChannel<ReliablePacket, SequencedRawMessage>
    {
        private const float RTT = 0.2f;

        private readonly ReliableRawMessageTracker _outgoingRawMessageTracker = new ReliableRawMessageTracker();
        private readonly Dictionary<SeqNo, SequencedRawMessage> _outgoingRawMessageQueue = new Dictionary<SeqNo, SequencedRawMessage>();
        private SeqNo _outgoingRawMessageSeq = SeqNo.Zero;

        private readonly Dictionary<SeqNo, SequencedRawMessage> _incomingRawMessageQueue = new Dictionary<SeqNo, SequencedRawMessage>();
        private SeqNo _incomingReadRawMessageSeq = SeqNo.Zero;
        private SeqNo _incomingRawMessageSeq = SeqNo.Zero;

        private SeqNo _outgoingPacketSeq = SeqNo.Zero;
        private SeqNo _incomingPacketAck = SeqNo.Zero - 1;
        private BitVector _incomingPacketAckBuffer = new BitVector(ReliablePacket.ChannelAckBufferLength);

        private bool _requireAcknowledgement;

        public ReliableOrderedChannel(Connection connection) : base(connection)
        {
        }

        public override IList<byte[]> GetReceivedMessages()
        {
            var receivedMessages = new List<byte[]>();
            while (_incomingRawMessageQueue.Remove(_incomingReadRawMessageSeq, out var rawMessage))
            {
                receivedMessages.Add(rawMessage.Data);
                _incomingReadRawMessageSeq++;
            }
            return receivedMessages;
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

        protected override bool AcceptIncomingRawMessage(SequencedRawMessage rawMessage)
        {
            if (rawMessage.Seq == _incomingRawMessageSeq)
            {
                // New message
                _incomingRawMessageSeq++;
                return true;
            }
            else if (rawMessage.Seq > _incomingRawMessageSeq)
            {
                if (_incomingRawMessageQueue.ContainsKey(rawMessage.Seq))
                {
                    // Already received messages
                    return false;
                }
                else
                {
                    // Early message
                    return true;
                }
            }
            else
            {
                // Late or already received messages
                return false;
            }
        }

        protected override void OnIncomingPacket(ReliablePacket packet)
        {
            // Packets without messages are ack packets
            // so we send ack only for received packets with messages
            _requireAcknowledgement = packet.RawMessages.Count > 0;
        }

        protected override void OnIncomingRawMessage(SequencedRawMessage rawMessage)
        {
            _incomingRawMessageQueue[rawMessage.Seq] = rawMessage;
        }


        protected override IList<ReliablePacket> PackOutgoingRawMessages(List<SequencedRawMessage> rawMessages)
        {
            var packets = base.PackOutgoingRawMessages(rawMessages);

            if (_requireAcknowledgement)
            {
                _requireAcknowledgement = false;

                // Send at least one packet with acks
                if (packets.Count == 0)
                {
                    var ackPacket = CreateOutgoingPacket();
                    packets.Add(ackPacket);
                }
            }

            return packets;
        }

        protected override List<SequencedRawMessage> GetOutgoingRawMessages()
        {
            var now = Timestamp.Current;
            lock (_outgoingRawMessageQueue)
            {
                if (_outgoingRawMessageQueue.Count == 0)
                {
                    return new List<SequencedRawMessage>();
                }
                var retransmissionTimeout = now - (long)(_connection.RTT * RTT);
                return _outgoingRawMessageQueue.Values
                    .Where(x => !x.Timestamp.HasValue || x.Timestamp.Value < retransmissionTimeout)
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

        protected override void PrepareOutgoingRawMessage(SequencedRawMessage rawMessage)
        {
            rawMessage.Seq = _outgoingRawMessageSeq++;
        }

        protected override void OnOutgoingPacket(ReliablePacket packet)
        {
            _outgoingRawMessageTracker.Track(packet.Seq, packet.RawMessages.Select(x => x.Seq));
        }

        protected override void OnOutgoingRawMessage(SequencedRawMessage rawMessage)
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
                // Already received packet
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
                    // New packet
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
                    // Late packet
                    return false;
                }
                else
                {
                    var ackIndex = diff - 1;
                    if (_incomingPacketAckBuffer[ackIndex])
                    {
                        // Already received packet
                        return false;
                    }
                    else
                    {
                        // New packet
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
            var rawMessageSeqs = _outgoingRawMessageTracker.Clear(ack);
            if (rawMessageSeqs != null)
            {
                lock (_outgoingRawMessageQueue)
                {
                    foreach (var rawMessageSeq in rawMessageSeqs)
                    {
                        _outgoingRawMessageQueue.Remove(rawMessageSeq);
                    }
                }
            }
        }
    }
}
