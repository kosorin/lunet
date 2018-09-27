using Lure.Net.Data;
using Lure.Net.Packets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lure.Net.Channels.Message
{
    public class UnreliableChannel : INetChannel
    {
        private readonly Connection _connection;

        private readonly Func<UnreliablePacket> _packetActivator;
        private readonly Func<UnreliableMessage> _messageActivator;
        private readonly SourceOrderMessagePacker<UnreliablePacket, UnreliableMessage> _messagePacker;

        private readonly List<UnreliableMessage> _outgoingMessageQueue = new List<UnreliableMessage>();
        private readonly List<UnreliableMessage> _incomingMessageQueue = new List<UnreliableMessage>();

        public UnreliableChannel(Connection connection)
        {
            _connection = connection;

            _messageActivator = ObjectActivatorFactory.Create<UnreliableMessage>();
            _packetActivator = ObjectActivatorFactory.CreateWithValues<Func<UnreliableMessage>, UnreliablePacket>(_messageActivator);
            _messagePacker = new SourceOrderMessagePacker<UnreliablePacket, UnreliableMessage>(_packetActivator);
        }


        public void ProcessIncomingPacket(NetDataReader reader)
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

        public IList<INetPacket> CollectOutgoingPackets()
        {
            var outgoingMessages = GetOutgoingMessages();
            if (outgoingMessages == null)
            {
                return null;
            }

            var outgoingPackets = _messagePacker.Pack(outgoingMessages, _connection.MTU);
            if (outgoingPackets == null)
            {
                return null;
            }

            return outgoingPackets.Cast<INetPacket>().ToList();
        }

        public IList<byte[]> GetReceivedMessages()
        {
            lock (_incomingMessageQueue)
            {
                var receivedMessages = _incomingMessageQueue.Select(x => x.Data).ToList();
                _incomingMessageQueue.Clear();
                return receivedMessages;
            }
        }

        public void SendMessage(byte[] data)
        {
            lock (_outgoingMessageQueue)
            {
                var message = _messageActivator();
                message.Data = data;
                _outgoingMessageQueue.Add(message);
            }
        }


        private List<UnreliableMessage> GetOutgoingMessages()
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
