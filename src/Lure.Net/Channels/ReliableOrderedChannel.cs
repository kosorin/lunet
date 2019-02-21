using Lure.Extensions.NetCore;
using Lure.Net.Data;
using Lure.Net.Logging;
using System.Collections.Generic;
using System.Linq;

namespace Lure.Net.Channels
{
    public class ReliableOrderedChannel : MessageChannel<ReliablePacket, ReliableMessage>
    {
        private readonly object _packetLock = new object();
        private SeqNo _outgoingPacketSeq = SeqNo.Zero;
        private SeqNo _incomingPacketAck = SeqNo.Zero - 1;
        private BitVector _incomingPacketAckBuffer = new BitVector(AckBufferLength);
        private bool _requireAckPacket;

        private readonly ReliableMessageTracker _outgoingMessageTracker = new ReliableMessageTracker();
        private readonly Dictionary<SeqNo, ReliableMessage> _outgoingMessageQueue = new Dictionary<SeqNo, ReliableMessage>();
        private SeqNo _outgoingMessageSeq = SeqNo.Zero;

        private readonly Dictionary<SeqNo, ReliableMessage> _incomingMessageQueue = new Dictionary<SeqNo, ReliableMessage>();
        private SeqNo _incomingReadMessageSeq = SeqNo.Zero;
        private SeqNo _incomingMessageSeq = SeqNo.Zero;

        public ReliableOrderedChannel(byte id, IConnection connection) : base(id, connection)
        {
        }


        public static int AckBufferLength { get; } = 128;

        //private static ILog Log { get; } = LogProvider.For<ReliableOrderedChannel>();


        public override void HandleIncomingPacket(NetDataReader reader)
        {
            var packet = PacketActivator();

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

        public override IList<IPacket> CollectOutgoingPackets()
        {
            var outgoingMessages = CollectOutgoingMessages();
            var outgoingPackets = MessagePacker.Pack(outgoingMessages, Connection.MTU);

            lock (_packetLock)
            {
                if (outgoingPackets == null)
                {
                    if (_requireAckPacket)
                    {
                        // Send at least one packet with acks
                        outgoingPackets = new[] { PacketActivator() };
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
                    packet.AckBuffer = _incomingPacketAckBuffer.Clone(0, ReliablePacket.AckBufferLength);
                    _outgoingMessageTracker.Track(packet.Seq, packet.Messages.Select(x => x.Seq));

                    foreach (var message in packet.Messages)
                    {
                        message.Timestamp = now;
                    }

                    //Log.Trace("[OUT] PACKET: Messages {Seq} {Messages}", packet.Seq, packet.Messages.Select(x => x.Seq).ToList());
                }
            }

            return outgoingPackets.Cast<IPacket>().ToList();
        }

        public override IList<byte[]> GetReceivedMessages()
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

        public override void SendMessage(byte[] data)
        {
            lock (_outgoingMessageQueue)
            {
                var message = MessageActivator();
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
                //Log.Trace("[IN] PACKET: Already {Seq}", seq);
                return false;
            }
            else if (diff > 0)
            {
                _incomingPacketAck = seq;

                if (diff > _incomingPacketAckBuffer.Capacity)
                {
                    // Early packet
                    //Log.Trace("[IN] PACKET: New early {Seq}", seq);
                    _incomingPacketAckBuffer.ClearAll();
                }
                else
                {
                    // New packet
                    //Log.Trace("[IN] PACKET: New early {Seq}", seq);
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
                    //Log.Trace("[IN] PACKET: Late {Seq}", seq);
                    return false;
                }
                else
                {
                    var ackIndex = diff - 1;
                    if (_incomingPacketAckBuffer[ackIndex])
                    {
                        // Already received packet
                        //Log.Trace("[IN] PACKET: Already {Seq}", seq);
                        return false;
                    }
                    else
                    {
                        // New packet
                        //Log.Trace("[IN] PACKET: New late {Seq}", seq);
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
                //Log.Trace("[IN] MESSAGE: New {Seq}", message.Seq);
                _incomingMessageSeq++;
                return true;
            }
            else if (message.Seq > _incomingMessageSeq)
            {
                if (_incomingMessageQueue.ContainsKey(message.Seq))
                {
                    // Already received messages
                    //Log.Trace("[IN] MESSAGE: Already received {Seq}; Expected {ExpectedSeq}", message.Seq, _incomingMessageSeq);
                    return false;
                }
                else
                {
                    // Early message
                    //Log.Trace("[IN] MESSAGE: Early {Seq}; Expected {ExpectedSeq}", message.Seq, _incomingMessageSeq);
                    return true;
                }
            }
            else
            {
                // Late or already received messages
                //Log.Trace("[IN] MESSAGE: Late {Seq}; Expected {ExpectedSeq}", message.Seq, _incomingMessageSeq);
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
                    var retransmissionTimeout = now - Connection.RTT;
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
