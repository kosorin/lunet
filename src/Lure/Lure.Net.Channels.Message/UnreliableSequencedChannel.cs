using Lure.Net.Packets;
using System.Collections.Generic;
using System.Linq;

namespace Lure.Net.Channels.Message
{
    public class UnreliableSequencedChannel : MessageChannel<UnreliableSequencedPacket, Message>
    {
        private readonly List<Message> _outgoingMessageQueue = new List<Message>();
        private readonly List<Message> _incomingMessageQueue = new List<Message>();

        private SeqNo _outgoingPacketSeq = SeqNo.Zero;
        private SeqNo _incomingPacketSeq = SeqNo.Zero - 1;

        public UnreliableSequencedChannel(Connection connection) : base(connection)
        {
        }

        public override IList<byte[]> GetReceivedMessages()
        {
            var receivedMessages = _incomingMessageQueue.Select(x => x.Data).ToList();
            _incomingMessageQueue.Clear();
            return receivedMessages;
        }


        protected override bool AcceptIncomingPacket(UnreliableSequencedPacket packet)
        {
            if (_incomingPacketSeq < packet.Seq)
            {
                // New packet
                _incomingPacketSeq = packet.Seq;
                return true;
            }
            else
            {
                // Late packet
                return false;
            }
        }

        protected override bool AcceptIncomingMessage(Message message)
        {
            return true;
        }

        protected override void OnIncomingPacket(UnreliableSequencedPacket packet)
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

        protected override void PrepareOutgoingPacket(UnreliableSequencedPacket packet)
        {
            packet.Seq = _outgoingPacketSeq++;
        }

        protected override void PrepareOutgoingMessage(Message message)
        {
        }

        protected override void OnOutgoingPacket(UnreliableSequencedPacket packet)
        {
        }

        protected override void OnOutgoingMessage(Message message)
        {
            lock (_outgoingMessageQueue)
            {
                _outgoingMessageQueue.Add(message);
            }
        }

        protected override bool AcknowledgeIncomingPacket(UnreliableSequencedPacket packet)
        {
            throw new System.NotImplementedException();
        }
    }
}
