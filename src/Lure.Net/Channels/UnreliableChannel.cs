using Lure.Net.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lure.Net.Channels
{
    public class UnreliableChannel : MessageChannel<UnreliablePacket, UnreliableMessage>
    {
        private readonly List<UnreliableMessage> _outgoingMessageQueue = new List<UnreliableMessage>();
        private readonly List<UnreliableMessage> _incomingMessageQueue = new List<UnreliableMessage>();

        public UnreliableChannel(byte id, IConnection connection) : base(id, connection)
        {
        }


        public override void HandleIncomingPacket(NetDataReader reader)
        {
            var packet = PacketActivator();

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

            var outgoingPackets = MessagePacker.Pack(outgoingMessages, Connection.MTU);
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
                var message = MessageActivator();
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
