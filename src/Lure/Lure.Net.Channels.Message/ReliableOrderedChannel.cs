using Lure.Extensions.NetCore;
using Lure.Net.Data;
using Lure.Net.Packets;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lure.Net.Channels.Message
{
    public class ReliableOrderedChannel : INetChannel
    {
        private const float RTT = 0.2f;

        private readonly Connection _connection;

        private readonly Func<ReliablePacket> _packetActivator;
        private readonly Func<ReliableMessage> _messageActivator;
        private readonly SourceOrderMessagePacker<ReliablePacket, ReliableMessage> _messagePacker;

        private readonly object _packetLock = new object();
        private SeqNo _outgoingPacketSeq = SeqNo.Zero;
        private SeqNo _incomingPacketAck = SeqNo.Zero - 1;
        private BitVector _incomingPacketAckBuffer = new BitVector(ReliablePacket.ChannelAckBufferLength);
        private bool _requireAckPacket;

        private readonly ReliableMessageTracker _outgoingMessageTracker = new ReliableMessageTracker();
        private readonly Dictionary<SeqNo, ReliableMessage> _outgoingMessageQueue = new Dictionary<SeqNo, ReliableMessage>();
        private SeqNo _outgoingMessageSeq = SeqNo.Zero;

        private readonly Dictionary<SeqNo, ReliableMessage> _incomingMessageQueue = new Dictionary<SeqNo, ReliableMessage>();
        private SeqNo _incomingReadMessageSeq = SeqNo.Zero;
        private SeqNo _incomingMessageSeq = SeqNo.Zero;

        public ReliableOrderedChannel(Connection connection)
        {
            _connection = connection;

            _messageActivator = ObjectActivatorFactory.Create<ReliableMessage>();
            _packetActivator = ObjectActivatorFactory.CreateWithValues<Func<ReliableMessage>, ReliablePacket>(_messageActivator);
            _messagePacker = new SourceOrderMessagePacker<ReliablePacket, ReliableMessage>(_packetActivator);

            Logger = Log.ForContext<ReliableOrderedChannel>();
        }


        public ILogger Logger { get; }


        public void ProcessIncomingPacket(NetDataReader reader)
        {
            var packet = _packetActivator();

            try
            {
                packet.DeserializeHeader(reader);
            }
            catch (NetSerializationException)
            {
                return;
            }

            lock (_packetLock)
            {
                if (!AcceptIncomingPacket(packet.Seq))
                {
                    return;
                }

                try
                {
                    packet.DeserializeData(reader);
                }
                catch (NetSerializationException)
                {
                    return;
                }

                AcknowledgeOutgoingPackets(packet.Ack, packet.AckBuffer);

                // Packets without messages are ack packets
                // so we send ack only for received packets with messages
                _requireAckPacket = packet.Messages.Count > 0;
            }

            if (packet.Messages.Count == 0)
            {
                return;
            }
            SaveIncomingMessages(packet.Messages);
        }

        public IList<INetPacket> CollectOutgoingPackets()
        {
            var outgoingMessages = CollectOutgoingMessages();
            if (outgoingMessages == null)
            {
                return null;
            }

            var outgoingPackets = _messagePacker.Pack(outgoingMessages, _connection.MTU);

            lock (_packetLock)
            {
                if (outgoingPackets == null)
                {
                    if (_requireAckPacket)
                    {
                        // Send at least one packet with acks
                        outgoingPackets = new List<ReliablePacket> { _packetActivator() };
                    }
                    else
                    {
                        return null;
                    }
                }
                _requireAckPacket = false;

                var now = Timestamp.Current;
                foreach (var packet in outgoingPackets)
                {
                    packet.Seq = _outgoingPacketSeq++;
                    packet.Ack = _incomingPacketAck;
                    packet.AckBuffer = _incomingPacketAckBuffer.Clone(0, ReliablePacket.PacketAckBufferLength);
                    _outgoingMessageTracker.Track(packet.Seq, packet.Messages.Select(x => x.Seq));

                    foreach (var message in packet.Messages)
                    {
                        message.Timestamp = now;
                    }

                    Logger.Error("PACKET: OUT Messages {Seq} {Messages}", packet.Seq, packet.Messages.Select(x => x.Seq).ToList());
                }
            }

            return outgoingPackets.Cast<INetPacket>().ToList();
        }

        public IList<byte[]> GetReceivedMessages()
        {
            lock (_incomingMessageQueue)
            {
                var receivedMessages = new List<byte[]>();
                while (_incomingMessageQueue.Remove(_incomingReadMessageSeq, out var message))
                {
                    receivedMessages.Add(message.Data);
                    _incomingReadMessageSeq++;
                }
                _incomingMessageSeq = _incomingReadMessageSeq;
                return receivedMessages;
            }
        }

        public void SendMessage(byte[] data)
        {
            lock (_outgoingMessageQueue)
            {
                var message = _messageActivator();
                message.Seq = _outgoingMessageSeq++;
                message.Data = data;
                message.Timestamp = null;
                if (!_outgoingMessageQueue.TryAdd(message.Seq, message))
                {
                    throw new NetException("Message buffer overflow.");
                }
            }
        }


        private bool AcceptIncomingPacket(SeqNo seq)
        {
            var diff = seq.CompareTo(_incomingPacketAck);
            if (diff == 0)
            {
                // Already received packet
                Logger.Warning("PACKET: Already {Seq}", seq);
                return false;
            }
            else if (diff > 0)
            {
                _incomingPacketAck = seq;

                if (diff > _incomingPacketAckBuffer.Capacity)
                {
                    // Early packet
                    Logger.Verbose("PACKET: New early {Seq}", seq);
                    _incomingPacketAckBuffer.ClearAll();
                }
                else
                {
                    // New packet
                    Logger.Verbose("PACKET: New early {Seq}", seq);
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
                    Logger.Warning("PACKET: Late {Seq}", seq);
                    return false;
                }
                else
                {
                    var ackIndex = diff - 1;
                    if (_incomingPacketAckBuffer[ackIndex])
                    {
                        // Already received packet
                        Logger.Warning("PACKET: Already {Seq}", seq);
                        return false;
                    }
                    else
                    {
                        // New packet
                        Logger.Verbose("PACKET: New late {Seq}", seq);
                        _incomingPacketAckBuffer.Set(diff - 1);
                        return true;
                    }
                }
            }
        }

        private bool AcceptIncomingMessage(ReliableMessage message)
        {
            if (message.Seq == _incomingMessageSeq)
            {
                // New message
                Logger.Verbose("MESSAGE: New {Seq}", message.Seq);
                _incomingMessageSeq++;
                return true;
            }
            else if (message.Seq > _incomingMessageSeq)
            {
                if (_incomingMessageQueue.ContainsKey(message.Seq))
                {
                    // Already received messages
                    Logger.Verbose("MESSAGE: Already received {Seq}; Expected {ExpectedSeq}", message.Seq, _incomingMessageSeq);
                    return false;
                }
                else
                {
                    // Early message
                    Logger.Verbose("MESSAGE: Early {Seq}; Expected {ExpectedSeq}", message.Seq, _incomingMessageSeq);
                    return true;
                }
            }
            else
            {
                // Late or already received messages
                Logger.Verbose("MESSAGE: Late {Seq}; Expected {ExpectedSeq}", message.Seq, _incomingMessageSeq);
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
            lock (_outgoingMessageQueue)
            {
                var messageSeqs = _outgoingMessageTracker.Clear(ack);
                if (messageSeqs != null)
                {
                    foreach (var messageSeq in messageSeqs)
                    {
                        _outgoingMessageQueue.Remove(messageSeq);
                    }
                }
            }
        }

        private void SaveIncomingMessages(List<ReliableMessage> messages)
        {
            lock (_incomingMessageQueue)
            {
                var now = Timestamp.Current;
                foreach (var message in messages)
                {
                    message.Timestamp = now;
                    if (AcceptIncomingMessage(message))
                    {
                        _incomingMessageQueue[message.Seq] = message;
                    }
                }
            }
        }

        private List<ReliableMessage> CollectOutgoingMessages()
        {
            lock (_outgoingMessageQueue)
            {
                if (_outgoingMessageQueue.Count > 0)
                {
                    var now = Timestamp.Current;
                    var retransmissionTimeout = now - (long)(_connection.RTT * RTT);
                    return _outgoingMessageQueue.Values
                        .Where(x => !x.Timestamp.HasValue || x.Timestamp.Value < retransmissionTimeout)
                        .OrderBy(x => x.Timestamp ?? long.MaxValue)
                        .ToList();
                }
                else
                {
                    return null;
                }
            }
        }
    }
}
