using Lure.Extensions.NetCore;
using Lure.Net.Data;
using Lure.Net.Packets;
using System.Collections.Generic;
using System.Linq;

namespace Lure.Net.Channels.Message
{
    public class ReliableOrderedChannel : MessageChannel<ReliablePacket, ReliableMessage>
    {
        private const float RTT = 0.2f;

        private readonly ReliableMessageTracker _outgoingMessageTracker = new ReliableMessageTracker();
        private readonly Dictionary<SeqNo, ReliableMessage> _outgoingMessageQueue = new Dictionary<SeqNo, ReliableMessage>();
        private SeqNo _outgoingMessageSeq = SeqNo.Zero;

        private readonly Dictionary<SeqNo, ReliableMessage> _incomingMessageQueue = new Dictionary<SeqNo, ReliableMessage>();
        private SeqNo _incomingReadMessageSeq = SeqNo.Zero;
        private SeqNo _incomingMessageSeq = SeqNo.Zero;

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
            while (_incomingMessageQueue.Remove(_incomingReadMessageSeq, out var message))
            {
                receivedMessages.Add(message.Data);
                _incomingReadMessageSeq++;
            }
            return receivedMessages;
        }


        protected override bool AcceptIncomingPacket(ReliablePacket packet)
        {
            var seq = packet.Seq;
            var diff = seq.CompareTo(_incomingPacketAck);
            if (diff == 0)
            {
                // Already received packet
                Logger.Warning("PACKET ALREADY {Seq}", seq);
                return false;
            }
            else if (diff > 0)
            {
                // Early/new packet
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
                        return true;
                    }
                }
            }
        }

        protected override bool AcceptIncomingMessage(ReliableMessage message)
        {
            if (message.Seq == _incomingMessageSeq)
            {
                // New message
                Logger.Verbose("MESSAGE NEW {Seq}", message.Seq);
                _incomingMessageSeq++;
                return true;
            }
            else if (message.Seq > _incomingMessageSeq)
            {
                if (_incomingMessageQueue.ContainsKey(message.Seq))
                {
                    // Already received messages
                    Logger.Verbose("MESSAGE ALREADY {Seq}", message.Seq);
                    return false;
                }
                else
                {
                    // Early message
                    Logger.Verbose("MESSAGE EARLY {Seq}", message.Seq);
                    return true;
                }
            }
            else
            {
                // Late or already received messages
                Logger.Verbose("MESSAGE LATE/ALREADY {Seq}", message.Seq);
                return false;
            }
        }

        protected override void OnIncomingPacket(ReliablePacket packet)
        {
            // Packets without messages are ack packets
            // so we send ack only for received packets with messages
            _requireAcknowledgement = packet.Messages.Count > 0;
        }

        protected override void OnIncomingMessage(ReliableMessage message)
        {
            _incomingMessageQueue[message.Seq] = message;
        }

        protected override bool AcknowledgeIncomingPacket(ReliablePacket packet)
        {
            if (AcknowledgeIncomingPacket(packet.Seq))
            {
                AcknowledgeOutgoingPackets(packet.Ack, packet.AckBuffer);
                return true;
            }
            return false;
        }


        protected override List<ReliablePacket> PackOutgoingMessages(List<ReliableMessage> messages)
        {
            var packets = base.PackOutgoingMessages(messages);

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

        protected override List<ReliableMessage> GetOutgoingMessages()
        {
            var now = Timestamp.Current;
            lock (_outgoingMessageQueue)
            {
                if (_outgoingMessageQueue.Count == 0)
                {
                    return new List<ReliableMessage>();
                }
                var retransmissionTimeout = now - (long)(_connection.RTT * RTT);
                return _outgoingMessageQueue.Values
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

        protected override void PrepareOutgoingMessage(ReliableMessage message)
        {
            message.Seq = _outgoingMessageSeq++;
        }

        protected override void OnOutgoingPacket(ReliablePacket packet)
        {
            Logger.Verbose("OUT PACKET {Seq} : {MessageSeqs}", packet.Seq, packet.Messages.Select(x => x.Seq));
            _outgoingMessageTracker.Track(packet.Seq, packet.Messages.Select(x => x.Seq));
        }

        protected override void OnOutgoingMessage(ReliableMessage message)
        {
            lock (_outgoingMessageQueue)
            {
                Logger.Verbose("OUT MESSAGE {Seq}", message.Seq);
                if (!_outgoingMessageQueue.TryAdd(message.Seq, message))
                {
                    throw new NetException("Message buffer overflow.");
                }
            }
        }


        private bool AcknowledgeIncomingPacket(SeqNo seq)
        {
            var diff = seq.CompareTo(_incomingPacketAck);
            if (diff == 0)
            {
                // Already received packet
                Logger.Warning("PACKET ALREADY {Seq}", seq);
                return false;
            }
            else if (diff > 0)
            {
                _incomingPacketAck = seq;

                if (diff > _incomingPacketAckBuffer.Capacity)
                {
                    // Early packet
                    Logger.Verbose("PACKET EARLY {Seq}", seq);
                    _incomingPacketAckBuffer.ClearAll();
                }
                else
                {
                    // New packet
                    Logger.Verbose("PACKET NEW {Seq}", seq);
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
                    Logger.Warning("PACKET LATE x {Seq}", seq);
                    return false;
                }
                else
                {
                    var ackIndex = diff - 1;
                    if (_incomingPacketAckBuffer[ackIndex])
                    {
                        // Already received packet
                        Logger.Warning("PACKET ALREADY x {Seq}", seq);
                        return false;
                    }
                    else
                    {
                        // New packet
                        Logger.Verbose("PACKET NEW x {Seq}", seq);
                        _incomingPacketAckBuffer.Set(diff - 1);
                        return true;
                    }
                }
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
            var messageSeqs = _outgoingMessageTracker.Clear(ack);
            if (messageSeqs != null)
            {
                lock (_outgoingMessageQueue)
                {
                    foreach (var messageSeq in messageSeqs)
                    {
                        _outgoingMessageQueue.Remove(messageSeq);
                    }
                }
            }
        }
    }
}
