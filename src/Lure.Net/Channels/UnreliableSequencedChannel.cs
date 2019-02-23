using Lure.Net.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lure.Net.Channels
{
    public class UnreliableSequencedChannel : MessageChannel<UnreliableSequencedPacket, UnreliableMessage>
    {
        private readonly List<UnreliableMessage> _outgoingMessageQueue = new List<UnreliableMessage>();
        private readonly object _outgoingPacketSeqLock = new object();
        private SeqNo _outgoingPacketSeq = SeqNo.Zero;

        private readonly List<UnreliableMessage> _incomingMessageQueue = new List<UnreliableMessage>();
        private readonly object _incomingPacketSeqLock = new object();
        private SeqNo _incomingPacketSeq = SeqNo.Zero - 1;

        public UnreliableSequencedChannel(byte id, IConnection connection) : base(id, connection)
        {
        }


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

        public override IList<IChannelPacket> CollectOutgoingPackets()
        {
            var outgoingMessages = CollectOutgoingMessages();
            if (outgoingMessages == null)
            {
                return null;
            }

            var outgoingPackets = MessagePacker.Pack(outgoingMessages, Connection.MTU);
            if (outgoingPackets == null)
            {
                return null;
            }

            lock (_outgoingPacketSeqLock)
            {
                foreach (var packet in outgoingPackets)
                {
                    packet.Seq = _outgoingPacketSeq++;
                }
            }

            return outgoingPackets.Cast<IChannelPacket>().ToList();
        }

        public override IList<byte[]> GetReceivedMessages()
        {
            lock (_incomingMessageQueue)
            {
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

        private List<UnreliableMessage> CollectOutgoingMessages()
        {
            lock (_outgoingMessageQueue)
            {
                if (_outgoingMessageQueue.Count > 0)
                {
                    var messages = _outgoingMessageQueue.ToList();
                    _outgoingMessageQueue.Clear();
                    return messages;
                }
                else
                {
                    return null;
                }
            }
        }
    }
}
