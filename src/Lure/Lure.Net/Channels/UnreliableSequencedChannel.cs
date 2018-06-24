using Lure.Net.Data;
using Lure.Net.Messages;
using Lure.Net.Packets.Message;
using Serilog;
using System.Collections.Generic;
using System.Linq;

namespace Lure.Net.Channels
{
    internal class UnreliableSequencedChannel : MessageChannel<UnreliableSequencedPacket, UnreliableRawMessage>
    {
        private readonly List<UnreliableRawMessage> _outgoingRawMessageQueue = new List<UnreliableRawMessage>();

        private SeqNo _outgoingPacketSeq = SeqNo.Zero;
        private SeqNo _incomingPacketSeq = SeqNo.Zero - 1;

        public UnreliableSequencedChannel(byte id, NetConnection connection) : base(id, connection)
        {
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

        protected override bool AcceptIncomingRawMessage(UnreliableRawMessage rawMessage)
        {
            return true;
        }

        protected override void OnIncomingPacket(UnreliableSequencedPacket packet)
        {
        }

        protected override void OnIncomingRawMessage(UnreliableRawMessage rawMessage)
        {
        }


        protected override List<UnreliableRawMessage> CollectOutgoingRawMessages()
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

        protected override void PrepareOutgoingPacket(UnreliableSequencedPacket packet)
        {
            packet.Seq = _outgoingPacketSeq++;
        }

        protected override void PrepareOutgoingRawMessage(UnreliableRawMessage rawMessage)
        {
        }

        protected override void OnOutgoingPacket(UnreliableSequencedPacket packet)
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
