using Lure.Net.Data;
using Lure.Net.Messages;
using Lure.Net.Packets;
using Lure.Net.Packets.Message;
using Serilog;
using System;
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

        public override void SendRawMessage(byte[] data)
        {
            var rawMessage = _rawMessagePool.Rent();
            rawMessage.Data = data;

            lock (_outgoingRawMessageQueue)
            {
                _outgoingRawMessageQueue.Add(rawMessage);
            }
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

        protected override void PrepareOutgoingPacket(UnreliableSequencedPacket packet)
        {
            packet.Seq = _outgoingPacketSeq++;
        }

        protected override List<UnreliableRawMessage> CollectOutgoingRawMessages()
        {
            lock (_outgoingRawMessageQueue)
            {
                if (_outgoingRawMessageQueue.Count == 0)
                {
                    return new List<UnreliableRawMessage>();
                }
                var rawMessages = _outgoingRawMessageQueue.ToList();
                _outgoingRawMessageQueue.Clear();
                return rawMessages;
            }
        }

        protected override void ParseRawMessages(UnreliableSequencedPacket packet)
        {
            foreach (var rawMessage in packet.RawMessages)
            {
                var reader = new NetDataReader(rawMessage.Data);
                var typeId = reader.ReadUShort();
                var message = NetMessageManager.Create(typeId);
                message.Deserialize(reader);

                Log.Information("  {Message}", message);
            }
        }
    }
}
