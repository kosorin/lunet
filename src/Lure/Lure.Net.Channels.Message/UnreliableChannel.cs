using Lure.Net.Packets;
using System.Collections.Generic;
using System.Linq;

namespace Lure.Net.Channels.Message
{
    public class UnreliableChannel : MessageChannel<UnreliablePacket, Message>
    {
        private readonly List<Message> _outgoingMessageQueue = new List<Message>();
        private readonly List<Message> _incomingMessageQueue = new List<Message>();

        public UnreliableChannel(Connection connection) : base(connection)
        {
        }

        public override IList<byte[]> GetReceivedMessages()
        {
            var receivedMessages = _incomingMessageQueue.Select(x => x.Data).ToList();
            _incomingMessageQueue.Clear();
            return receivedMessages;
        }


        protected override bool AcceptIncomingPacket(UnreliablePacket packet)
        {
            return true;
        }

        protected override bool AcceptIncomingMessage(Message message)
        {
            return true;
        }

        protected override void OnIncomingPacket(UnreliablePacket packet)
        {
        }

        protected override void OnIncomingMessage(Message message)
        {
            _incomingMessageQueue.Add(message);
        }


        protected override List<Message> GetOutgoingMessages()
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
                    return new List<Message>();
                }
            }
        }

        protected override void PrepareOutgoingPacket(UnreliablePacket packet)
        {
        }

        protected override void PrepareOutgoingMessage(Message message)
        {
        }

        protected override void OnOutgoingPacket(UnreliablePacket packet)
        {
        }

        protected override void OnOutgoingMessage(Message message)
        {
            lock (_outgoingMessageQueue)
            {
                _outgoingMessageQueue.Add(message);
            }
        }

        protected override bool AcknowledgeIncomingPacket(UnreliablePacket packet)
        {
            throw new System.NotImplementedException();
        }
    }
}
