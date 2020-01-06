using Lunet.Common;
using Lunet.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lunet.Channels
{
    public class UnreliableSequencedChannel : MessageChannel<UnreliableSequencedPacket, UnreliableMessage>
    {
        private readonly List<UnreliableMessage> _outgoingMessageQueue = new List<UnreliableMessage>();
        private readonly object _outgoingPacketSeqLock = new object();
        private SeqNo _outgoingPacketSeq = SeqNo.Zero;

        private readonly List<UnreliableMessage> _incomingMessageQueue = new List<UnreliableMessage>();
        private readonly object _incomingPacketSeqLock = new object();
        private SeqNo _incomingPacketSeq = SeqNo.Zero - 1;

        public UnreliableSequencedChannel(byte id, Connection connection) : base(id, connection)
        {
            MessageActivator = ObjectActivatorFactory.Create<UnreliableMessage>();
            PacketActivator = ObjectActivatorFactory.CreateWithValues<Func<UnreliableMessage>, UnreliableSequencedPacket>(MessageActivator);
            MessagePacker = new UnreliableMessagePacker<UnreliableSequencedPacket, UnreliableMessage>(PacketActivator);
        }

        protected override Func<UnreliableSequencedPacket> PacketActivator { get; }

        protected override Func<UnreliableMessage> MessageActivator { get; }

        protected override IMessagePacker<UnreliableSequencedPacket, UnreliableMessage> MessagePacker { get; }


        public override List<byte[]>? GetReceivedMessages()
        {
            lock (_incomingMessageQueue)
            {
                if (_incomingMessageQueue.Count == 0)
                {
                    return null;
                }

                var receivedMessages = _incomingMessageQueue.Select(x => x.Data).ToList();
                _incomingMessageQueue.Clear();
                return receivedMessages;
            }
        }

        public override void SendMessage(byte[] data)
        {
            lock (_outgoingMessageQueue)
            {
                var message = MessageActivator();
                message.Data = data;
                _outgoingMessageQueue.Add(message);
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

            if (packet.Messages.Count == 0)
            {
                return;
            }

            lock (_incomingMessageQueue)
            {
                foreach (var message in packet.Messages)
                {
                    _incomingMessageQueue.Add(message);
                }
            }
        }

        internal override List<ChannelPacket>? CollectOutgoingPackets()
        {
            var outgoingPackets = PackOutgoingPackets();

            if (outgoingPackets == null)
            {
                return null;
            }

            lock (_outgoingPacketSeqLock)
            {
                foreach (var packet in outgoingPackets)
                {
                    OnOutgoingPacket(packet);
                }
            }

            return outgoingPackets.Cast<ChannelPacket>().ToList();
        }


        protected override List<UnreliableMessage>? CollectOutgoingMessages()
        {
            lock (_outgoingMessageQueue)
            {
                if (_outgoingMessageQueue.Count == 0)
                {
                    return null;
                }

                var messages = _outgoingMessageQueue.ToList();
                _outgoingMessageQueue.Clear();
                return messages;
            }
        }


        private bool AcceptIncomingPacket(SeqNo seq)
        {
            lock (_incomingPacketSeqLock)
            {
                if (_incomingPacketSeq < seq)
                {
                    // New packet
                    _incomingPacketSeq = seq;
                    return true;
                }
                else
                {
                    // Late packet
                    return false;
                }
            }
        }


        private void OnOutgoingPacket(UnreliableSequencedPacket packet)
        {
            packet.Seq = _outgoingPacketSeq++;
        }
    }
}
