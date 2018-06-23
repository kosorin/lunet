using Lure.Net.Data;
using Lure.Net.Messages;
using Lure.Net.Packets.Message;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lure.Net.Channels
{
    internal class UnreliableChannel : MessageChannel<UnreliablePacket, UnreliableRawMessage>
    {
        private readonly List<UnreliableRawMessage> _outgoingRawMessageQueue = new List<UnreliableRawMessage>();

        public UnreliableChannel(byte id, NetConnection connection) : base(id, connection)
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

        protected override bool AcceptIncomingPacket(UnreliablePacket packet)
        {
            return true;
        }

        protected override void PrepareOutgoingPacket(UnreliablePacket packet)
        {
        }

        protected override List<UnreliableRawMessage> CollectOutgoingRawMessages()
        {
            lock (_outgoingRawMessageQueue)
            {
                var rawMessages = _outgoingRawMessageQueue.ToList();
                _outgoingRawMessageQueue.Clear();
                return rawMessages;
            }
        }

        protected override void ParseRawMessages(UnreliablePacket packet)
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
