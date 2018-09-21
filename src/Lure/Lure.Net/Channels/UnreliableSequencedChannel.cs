using Lure.Net.Packets;
using System.Collections.Generic;
using System.Linq;

namespace Lure.Net.Channels
{
    public class UnreliableSequencedChannel : NetChannel<UnreliableSequencedPacket, RawMessage>
    {
        private readonly List<RawMessage> _outgoingRawMessageQueue = new List<RawMessage>();
        private readonly List<RawMessage> _incomingRawMessageQueue = new List<RawMessage>();

        private SeqNo _outgoingPacketSeq = SeqNo.Zero;
        private SeqNo _incomingPacketSeq = SeqNo.Zero - 1;

        public UnreliableSequencedChannel(Connection connection) : base(connection)
        {
        }

        public override IList<byte[]> GetReceivedMessages()
        {
            var receivedMessages = _incomingRawMessageQueue.Select(x => x.Data).ToList();
            _incomingRawMessageQueue.Clear();
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

        protected override bool AcceptIncomingRawMessage(RawMessage rawMessage)
        {
            return true;
        }

        protected override void OnIncomingPacket(UnreliableSequencedPacket packet)
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

        protected override void PrepareOutgoingPacket(UnreliableSequencedPacket packet)
        {
            packet.Seq = _outgoingPacketSeq++;
        }

        protected override void PrepareOutgoingRawMessage(RawMessage rawMessage)
        {
        }

        protected override void OnOutgoingPacket(UnreliableSequencedPacket packet)
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
