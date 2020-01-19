using Lunet.Common;
using Lunet.Data;
using Lunet.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lunet.Channels
{
    public class ReliableOrderedChannel : MessageChannel<ReliablePacket, ReliableMessage>
    {
        private static ILog Log { get; } = LogProvider.GetCurrentClassLogger();


        private readonly object _packetLock = new object();
        private SeqNo _outgoingPacketSeq = SeqNo.Zero;
        private bool _isFirstIncomingPacketAck = true;
        private SeqNo _incomingPacketAck = SeqNo.Zero - 1;
        private readonly BitVector _incomingPacketAckBuffer = new BitVector(AckBufferLength);
        private bool _requireAckPacket;

        private readonly ReliableMessageTracker _outgoingMessageTracker = new ReliableMessageTracker(AckBufferLength * 2);
        private readonly Dictionary<SeqNo, ReliableMessage> _outgoingMessageQueue = new Dictionary<SeqNo, ReliableMessage>();
        private SeqNo _outgoingMessageSeq = SeqNo.Zero;

        private readonly Dictionary<SeqNo, ReliableMessage> _incomingMessageQueue = new Dictionary<SeqNo, ReliableMessage>();
        private SeqNo _incomingReadMessageSeq = SeqNo.Zero;
        private SeqNo _incomingMessageSeq = SeqNo.Zero;

        public ReliableOrderedChannel(byte id, Connection connection) : base(id, connection)
        {
            MessageActivator = ObjectActivatorFactory.Create<ReliableMessage>();
            PacketActivator = ObjectActivatorFactory.CreateWithValues<Func<ReliableMessage>, ReliablePacket>(MessageActivator);
            MessagePacker = new ReliableMessagePacker<ReliablePacket, ReliableMessage>(PacketActivator);
        }

        protected override Func<ReliablePacket> PacketActivator { get; }

        protected override Func<ReliableMessage> MessageActivator { get; }

        protected override IMessagePacker<ReliablePacket, ReliableMessage> MessagePacker { get; }


        public static int AckBufferLength { get; } = 32;


        public override List<byte[]>? GetReceivedMessages()
        {
            lock (_incomingMessageQueue)
            {
                // TODO: new
                var receivedMessages = new List<byte[]>();
                while (_incomingReadMessageSeq < _incomingMessageSeq && _incomingMessageQueue.Remove(_incomingReadMessageSeq, out var message))
                {
                    _incomingReadMessageSeq++;
                    receivedMessages.Add(message.Data);
                }
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
                message.SendTimestamp = null;
                if (!_outgoingMessageQueue.TryAdd(message.Seq, message))
                {
                    throw new NetException("Message buffer overflow.");
                }
            }
        }


        internal override void HandleIncomingPacket(NetDataReader reader)
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

                Log.Trace($"Received packet: {packet.Seq} => {string.Join(", ", packet.Messages.Select(x => x.Seq))}");
                Log.Trace($"Acked packets: {string.Join(", ", packet.AckBuffer.AsBits().Select((x, i) => x ? packet.Ack - i : (SeqNo?)null).Where(x => x != null))}");
                Log.Trace($"             : {packet.Ack} - {packet.AckBuffer}");
                AcknowledgeOutgoingPackets(packet.Ack, packet.AckBuffer!);

                if (!_requireAckPacket)
                {
                    // Packets without messages are ack packets
                    // so we send ack only for received packets with messages
                    _requireAckPacket = packet.Messages.Count > 0;
                }
            }

            if (packet.Messages.Count == 0)
            {
                return;
            }
            SaveIncomingMessages(packet.Messages);
        }

        internal override List<ChannelPacket>? CollectOutgoingPackets()
        {
            var outgoingPackets = PackOutgoingPackets();

            lock (_packetLock)
            {
                if (outgoingPackets == null)
                {
                    if (_requireAckPacket)
                    {
                        // Send at least one packet with acks
                        // TODO: new
                        outgoingPackets = new List<ReliablePacket> { PacketActivator() };
                    }
                    else
                    {
                        return null;
                    }
                }
                _requireAckPacket = false;

                var now = Timestamp.GetCurrent();
                foreach (var packet in outgoingPackets)
                {
                    OnOutgoingPacket(packet, now);
                }
            }

            return outgoingPackets.Cast<ChannelPacket>().ToList();
        }


        protected override List<ReliableMessage>? CollectOutgoingMessages()
        {
            List<ReliableMessage>? outgoingMessages = null;

            lock (_outgoingMessageQueue)
            {
                if (_outgoingMessageQueue.Count > 0)
                {
                    var now = Timestamp.GetCurrent();

                    foreach (var message in _outgoingMessageQueue.Values)
                    {
                        if (message.FirstSendTimestamp.HasValue && message.FirstSendTimestamp.Value + Connection.Timeout < now)
                        {
                            throw new Exception($"Outgoing reliable message {message.Seq} timeout.");
                        }
                        else if (message.SendTimestamp.HasValue && message.SendTimestamp.Value + (Connection.RTT * 2.5) >= now)
                        {
                            continue;
                        }

                        if (outgoingMessages == null)
                        {
                            outgoingMessages = new List<ReliableMessage>();
                        }
                        outgoingMessages.Add(message);
                    }
                }
                else
                {
                    return null;
                }
            }

            return outgoingMessages;
        }


        private bool AcceptIncomingPacket(SeqNo seq)
        {
            if (_isFirstIncomingPacketAck)
            {
                _incomingPacketAck = seq - 1;
                _isFirstIncomingPacketAck = false;
            }

            var diff = seq.CompareTo(_incomingPacketAck);
            if (diff == 0)
            {
                // Already received packet
                return false;
            }
            else if (diff > 0)
            {
                _incomingPacketAck = seq;

                if (diff < _incomingPacketAckBuffer.Capacity)
                {
                    // New packet
                    _incomingPacketAckBuffer.LeftShift(diff);
                }
                else
                {
                    // Early packet but still ok
                    _incomingPacketAckBuffer.ClearAll();
                }

                _incomingPacketAckBuffer.Set(0);

                return true;
            }
            else
            {
                diff *= -1;
                if (diff < _incomingPacketAckBuffer.Capacity)
                {
                    if (_incomingPacketAckBuffer[diff])
                    {
                        // Already received packet
                        return false;
                    }
                    else
                    {
                        // New packet
                        _incomingPacketAckBuffer.Set(diff);
                        return true;
                    }
                }
                else
                {
                    // Late packet
                    return false;
                }
            }
        }

        private void SaveIncomingMessages(List<ReliableMessage> messages)
        {
            lock (_incomingMessageQueue)
            {
                var now = Timestamp.GetCurrent();

                Span<SeqNo> input = stackalloc SeqNo[messages.Count];
                Span<SortItem> outputItems = stackalloc SortItem[messages.Count];

                for (var i = 0; i < messages.Count; i++)
                {
                    input[i] = messages[i].Seq;
                }
                var x = input.ToString();

                _incomingMessageSeq.Sort(input, outputItems);

                foreach (var item in outputItems)
                {
                    var message = messages[item.Index];
                    if (AcceptIncomingMessage(message))
                    {
                        OnIncomingMessage(message, now);
                    }
                }
            }
        }

        private bool AcceptIncomingMessage(ReliableMessage message)
        {
            if (message.Seq < _incomingMessageSeq)
            {
                // Late or already received messages
                return false;
            }
            else if (_incomingMessageQueue.TryAdd(message.Seq, message))
            {
                // New or early messages
                while (_incomingMessageSeq >= _incomingReadMessageSeq && _incomingMessageQueue.ContainsKey(_incomingMessageSeq))
                {
                    _incomingMessageSeq++;
                }
                return true;
            }
            else
            {
                // Already received messages
                return false;
            }
        }

        private void AcknowledgeOutgoingPackets(SeqNo ack, BitVector acks)
        {
            lock (_outgoingMessageQueue)
            {
                var currentAck = ack;
                foreach (var bit in acks.AsBits())
                {
                    if (bit)
                    {
                        AcknowledgeOutgoingPacket(currentAck);
                    }
                    currentAck--;
                }
            }
        }

        private void AcknowledgeOutgoingPacket(SeqNo ack)
        {
            lock (_outgoingMessageQueue)
            {
                var messages = _outgoingMessageTracker.Get(ack);
                if (messages != null)
                {
                    Log.Trace($"Acked packet: {ack} => {string.Join(", ", messages.Select(x => x.Seq))}");
                    foreach (var message in messages)
                    {
                        _outgoingMessageQueue.Remove(message.Seq);
                    }
                }
                _outgoingMessageTracker.Clear(ack);
            }
        }


        private static void OnIncomingMessage(ReliableMessage message, long now)
        {
            Log.Trace($"Receive message: {message.Seq}");
        }


        private void OnOutgoingPacket(ReliablePacket packet, long now)
        {
            packet.Seq = _outgoingPacketSeq++;
            packet.Ack = _incomingPacketAck;
            packet.AckBuffer = _incomingPacketAckBuffer.Clone(0, ReliablePacket.AckBufferLength);

            _outgoingMessageTracker.Track(packet.Seq, packet.Messages);

            Log.Trace($"Sending packet: {packet.Seq} => {string.Join(", ", packet.Messages.Select(x => x.Seq))}");
            Log.Trace($"Acking packets: {string.Join(", ", packet.AckBuffer.AsBits().Select((x, i) => x ? packet.Ack - i : (SeqNo?)null).Where(x => x != null))}");
            Log.Trace($"              : {packet.Ack} - {packet.AckBuffer}");
            foreach (var message in packet.Messages)
            {
                OnOutgoingMessage(message, now);
            }
        }

        private static void OnOutgoingMessage(ReliableMessage message, long now)
        {
            message.SendTimestamp = now;
        }
    }
}
