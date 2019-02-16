using Lure.Net.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lure.Net.Channels.Message
{
    public class UnreliableChannel : NetChannel
    {
        private readonly Func<UnreliablePacket> _packetActivator;
        private readonly Func<UnreliableMessage> _messageActivator;
        private readonly SourceOrderMessagePacker<UnreliablePacket, UnreliableMessage> _messagePacker;

        private readonly List<UnreliableMessage> _outgoingMessageQueue = new List<UnreliableMessage>();
        private readonly List<UnreliableMessage> _incomingMessageQueue = new List<UnreliableMessage>();

        public UnreliableChannel(byte id, Connection connection) : base(id, connection)
        {
            _messageActivator = ObjectActivatorFactory.Create<UnreliableMessage>();
            _packetActivator = ObjectActivatorFactory.CreateWithValues<Func<UnreliableMessage>, UnreliablePacket>(_messageActivator);
            _messagePacker = new SourceOrderMessagePacker<UnreliablePacket, UnreliableMessage>(_packetActivator);
        }


        public override void ProcessIncomingPacket(NetDataReader reader)
        {
            var packet = _packetActivator();

            try
            {
                packet.DeserializeHeader(reader);
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

        public override IList<IPacket> CollectOutgoingPackets()
        {
            var outgoingMessages = CollectOutgoingMessages();
            if (outgoingMessages == null)
            {
                return null;
            }

            var outgoingPackets = _messagePacker.Pack(outgoingMessages, Connection.MTU);
            if (outgoingPackets == null)
            {
                return null;
            }

            return outgoingPackets.Cast<IPacket>().ToList();
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
                var message = _messageActivator();
                message.Data = data;
                _outgoingMessageQueue.Add(message);
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
