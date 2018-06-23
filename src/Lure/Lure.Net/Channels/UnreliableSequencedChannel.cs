using Lure.Net.Data;
using Lure.Net.Messages;
using Lure.Net.Packets;
using Serilog;
using System;
using System.Collections.Generic;

namespace Lure.Net.Channels
{
    internal class UnreliableSequencedChannel : PayloadChannel<SequencedPacket, UnreliablePayloadPacketData>
    {
        private readonly Queue<byte[]> _outgoingRawMessageQueue = new Queue<byte[]>();

        private SeqNo _outgoingPacketSeq = SeqNo.Zero;
        private SeqNo _incomingPacketSeq = SeqNo.Zero - 1;

        public UnreliableSequencedChannel(byte id, NetConnection connection)
            : base(id, connection, PacketDataType.PayloadUnreliableSequenced)
        {
        }

        public override void SendRawMessage(byte[] rawMessage)
        {
            lock (_outgoingRawMessageQueue)
            {
                _outgoingRawMessageQueue.Enqueue(rawMessage);
            }
        }

        protected override bool AcceptIncomingPacket(SequencedPacket packet)
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

        protected override void PrepareOutgoingPacket(SequencedPacket packet)
        {
            packet.Seq = _outgoingPacketSeq++;
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

        protected override void ParseRawMessages(SequencedPacket packet)
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
