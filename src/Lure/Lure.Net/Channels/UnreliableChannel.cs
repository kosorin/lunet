using Lure.Net.Data;
using Lure.Net.Messages;
using Lure.Net.Packets;
using Serilog;
using System;
using System.Collections.Generic;

namespace Lure.Net.Channels
{
    internal class UnreliableChannel : PayloadChannel<UnreliablePacket, UnreliablePayloadPacketData>
    {
        private readonly Queue<byte[]> _outgoingRawMessageQueue = new Queue<byte[]>();

        public UnreliableChannel(byte id, NetConnection connection)
            : base(id, connection, PacketDataType.PayloadUnreliable)
        {
        }

        public override void SendRawMessage(byte[] rawMessage)
        {
            lock (_outgoingRawMessageQueue)
            {
                _outgoingRawMessageQueue.Enqueue(rawMessage);
            }
        }

        protected override bool AcceptIncomingPacket(UnreliablePacket packet)
        {
            return true;
        }

        protected override void PrepareOutgoingPacket(UnreliablePacket packet)
        {
        }

        protected override List<UnreliablePayloadPacketData> CollectOutgoingData()
        {
            var dataList = new List<UnreliablePayloadPacketData>();
            var data = _dataPool.Rent();

            lock (_outgoingRawMessageQueue)
            {
                while (_outgoingRawMessageQueue.Count > 0)
                {
                    var rawMessage = _outgoingRawMessageQueue.Dequeue();
                    if (data.Length + rawMessage.Length > _connection.MTU)
                    {
                        dataList.Add(data);
                        data = _dataPool.Rent();
                    }
                    data.RawMessages.Add(rawMessage);
                }
            }

            if (data.Length > 0)
            {
                dataList.Add(data);
            }
            return dataList;
        }

        protected override void ParseRawMessages(UnreliablePacket packet)
        {
            var data = (UnreliablePayloadPacketData)packet.Data;
            foreach (var rawMessage in data.RawMessages)
            {
                var reader = new NetDataReader(rawMessage);
                var typeId = reader.ReadUShort();
                var message = NetMessageManager.Create(typeId);
                message.Deserialize(reader);

                Log.Information("  {Message}", message);
            }
        }
    }
}
