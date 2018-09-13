using Lure.Net.Packets;
using System.Collections.Generic;
using System.Linq;

namespace Lure.Net.Channels
{
    internal class UnreliableSequencedChannel : NetChannel<UnreliableSequencedPacket, SequencedRawMessage>
    {
        private readonly List<SequencedRawMessage> _outgoingRawMessageQueue = new List<SequencedRawMessage>();
        private readonly List<SequencedRawMessage> _incomingRawMessageQueue = new List<SequencedRawMessage>();

        private SeqNo _outgoingPacketSeq = SeqNo.Zero;
        private SeqNo _incomingPacketSeq = SeqNo.Zero - 1;

        public UnreliableSequencedChannel(NetConnection connection) : base(connection)
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
                _incomingPacketSeq = packet.Seq;
                return true;
            }
            else
            {
                return false;
            }
        }

        protected override bool AcceptIncomingRawMessage(SequencedRawMessage rawMessage)
        {
            return true;
        }

        protected override void OnIncomingPacket(UnreliableSequencedPacket packet)
        {
        }

        protected override void OnIncomingRawMessage(SequencedRawMessage rawMessage)
        {
            _incomingRawMessageQueue.Add(rawMessage);
        }


        protected override List<SequencedRawMessage> GetOutgoingRawMessages()
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
                    return new List<SequencedRawMessage>();
                }
            }
        }

        protected override void PrepareOutgoingPacket(UnreliableSequencedPacket packet)
        {
            packet.Seq = _outgoingPacketSeq++;
        }

        protected override void PrepareOutgoingRawMessage(SequencedRawMessage rawMessage)
        {
        }

        protected override void OnOutgoingPacket(UnreliableSequencedPacket packet)
        {
        }

        protected override void OnOutgoingRawMessage(SequencedRawMessage rawMessage)
        {
            lock (_outgoingRawMessageQueue)
            {
                _outgoingRawMessageQueue.Add(rawMessage);
            }
        }
    }
}
