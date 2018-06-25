using Lure.Net.Packets.Message;
using System.Collections.Generic;
using System.Linq;

namespace Lure.Net.Channels
{
    internal class UnreliableChannel : MessageChannel<UnreliablePacket, UnreliableRawMessage>
    {
        private readonly List<UnreliableRawMessage> _outgoingRawMessageQueue = new List<UnreliableRawMessage>();
        private readonly List<UnreliableRawMessage> _incomingRawMessageQueue = new List<UnreliableRawMessage>();

        public UnreliableChannel(byte id, NetConnection connection) : base(id, connection)
        {
        }

        public override IEnumerable<RawMessage> GetReceivedRawMessages()
        {
            var receivedRawMessages = _incomingRawMessageQueue.ToList();
            _incomingRawMessageQueue.Clear();
            return receivedRawMessages;
        }


        protected override bool AcceptIncomingPacket(UnreliablePacket packet)
        {
            return true;
        }

        protected override bool AcceptIncomingRawMessage(UnreliableRawMessage rawMessage)
        {
            return true;
        }

        protected override void OnIncomingPacket(UnreliablePacket packet)
        {
        }

        protected override void OnIncomingRawMessage(UnreliableRawMessage rawMessage)
        {
            _incomingRawMessageQueue.Add(rawMessage);
        }


        protected override List<UnreliableRawMessage> GetOutgoingRawMessages()
        {
            lock (_outgoingRawMessageQueue)
            {
                if (_outgoingRawMessageQueue.Count > 0)
                {
                    var rawMessages = _outgoingRawMessageQueue.ToList();
                    _outgoingRawMessageQueue.Clear();
                    return rawMessages;
                }
                else
                {
                    return new List<UnreliableRawMessage>();
                }
            }
        }

        protected override void PrepareOutgoingPacket(UnreliablePacket packet)
        {
        }

        protected override void PrepareOutgoingRawMessage(UnreliableRawMessage rawMessage)
        {
        }

        protected override void OnOutgoingPacket(UnreliablePacket packet)
        {
        }

        protected override void OnOutgoingRawMessage(UnreliableRawMessage rawMessage)
        {
            lock (_outgoingRawMessageQueue)
            {
                _outgoingRawMessageQueue.Add(rawMessage);
            }
        }
    }
}
