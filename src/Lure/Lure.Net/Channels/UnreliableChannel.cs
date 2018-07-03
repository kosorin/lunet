using Lure.Net.Packets;
using System.Collections.Generic;
using System.Linq;

namespace Lure.Net.Channels
{
    internal class UnreliableChannel : NetChannel<UnreliablePacket, RawMessage>
    {
        private readonly List<RawMessage> _outgoingRawMessageQueue = new List<RawMessage>();
        private readonly List<RawMessage> _incomingRawMessageQueue = new List<RawMessage>();

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

        protected override bool AcceptIncomingRawMessage(RawMessage rawMessage)
        {
            return true;
        }

        protected override void OnIncomingPacket(UnreliablePacket packet)
        {
        }

        protected override void OnIncomingRawMessage(RawMessage rawMessage)
        {
            _incomingRawMessageQueue.Add(rawMessage);
        }


        protected override List<RawMessage> GetOutgoingRawMessages()
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
                    return new List<RawMessage>();
                }
            }
        }

        protected override void PrepareOutgoingPacket(UnreliablePacket packet)
        {
        }

        protected override void PrepareOutgoingRawMessage(RawMessage rawMessage)
        {
        }

        protected override void OnOutgoingPacket(UnreliablePacket packet)
        {
        }

        protected override void OnOutgoingRawMessage(RawMessage rawMessage)
        {
            lock (_outgoingRawMessageQueue)
            {
                _outgoingRawMessageQueue.Add(rawMessage);
            }
        }
    }
}
