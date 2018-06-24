using Lure.Collections;
using Lure.Net.Data;
using Lure.Net.Messages;
using Lure.Net.Packets;
using Lure.Net.Packets.Message;
using Serilog;
using System.Collections.Generic;

namespace Lure.Net.Channels
{
    internal abstract class MessageChannel<TPacket, TRawMessage> : NetChannel
        where TPacket : MessagePacket<TRawMessage>
        where TRawMessage : RawMessage
    {
        protected readonly ObjectPool<TPacket> _packetPool;
        protected readonly ObjectPool<TRawMessage> _rawMessagePool;

        private bool _disposed;

        protected MessageChannel(byte id, NetConnection connection) : base(id, connection)
        {
            _rawMessagePool = new ObjectPool<TRawMessage>();

            var packetActivator = ObjectActivatorFactory.Create<TPacket>(_rawMessagePool.GetType());
            _packetPool = new ObjectPool<TPacket>(() => packetActivator(_rawMessagePool));
        }

        public override void Update()
        {
            var outgoingRawMessages = CollectOutgoingRawMessages();
            var outgoingPackets = PackOutgoingRawMessages(outgoingRawMessages);
            foreach (var packet in outgoingPackets)
            {
                SendPacket(packet);
            }
        }

        public override void ReceivePacket(NetDataReader reader)
        {
            var packet = _packetPool.Rent();

            packet.DeserializeHeader(reader);

            if (!AcceptIncomingPacket(packet))
            {
                return;
            }

            packet.DeserializeData(reader);

            OnIncomingPacket(packet);

            var now = Timestamp.Current;
            foreach (var rawMessage in packet.RawMessages)
            {
                rawMessage.Timestamp = now;
                if (AcceptIncomingRawMessage(rawMessage))
                {
                    OnIncomingRawMessage(rawMessage);
                }
            }
            LastIncomingPacketTimestamp = now;

            _packetPool.Return(packet);
        }

        public void SendMessage(byte[] data)
        {
            var rawMessage = CreateOutgoingRawMessage(data);
            OnOutgoingRawMessage(rawMessage);
        }


        protected abstract bool AcceptIncomingPacket(TPacket packet);

        protected abstract bool AcceptIncomingRawMessage(TRawMessage rawMessage);

        protected abstract void OnIncomingPacket(TPacket packet);

        protected abstract void OnIncomingRawMessage(TRawMessage rawMessage);


        protected abstract List<TRawMessage> CollectOutgoingRawMessages();

        protected TPacket CreateOutgoingPacket()
        {
            var packet = _packetPool.Rent();
            packet.Direction = PacketDirection.Outgoing;
            packet.ChannelId = _id;

            PrepareOutgoingPacket(packet);

            return packet;
        }

        protected TRawMessage CreateOutgoingRawMessage(byte[] data)
        {
            var rawMessage = _rawMessagePool.Rent();
            rawMessage.Timestamp = null;
            rawMessage.Data = data;

            PrepareOutgoingRawMessage(rawMessage);

            return rawMessage;
        }

        protected abstract void PrepareOutgoingPacket(TPacket packet);

        protected abstract void PrepareOutgoingRawMessage(TRawMessage rawMessage);

        protected abstract void OnOutgoingPacket(TPacket packet);

        protected abstract void OnOutgoingRawMessage(TRawMessage rawMessage);


        protected void SendPacket(TPacket packet)
        {
            _connection.Peer.SendPacket(_connection, packet);

            OnOutgoingPacket(packet);

            var now = Timestamp.Current;
            foreach (var rawMessage in packet.RawMessages)
            {
                rawMessage.Timestamp = now;
            }
            LastOutgoingPacketTimestamp = now;

            _packetPool.Return(packet);
        }

        protected NetMessage ParseMessage(byte[] data)
        {
            var reader = new NetDataReader(data);
            var typeId = reader.ReadUShort();
            var message = NetMessageManager.Create(typeId);
            message.Deserialize(reader);
            return message;
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _packetPool.Dispose();
                    _rawMessagePool.Dispose();
                }
                _disposed = true;
            }
            base.Dispose(disposing);
        }


        private IEnumerable<TPacket> PackOutgoingRawMessages(List<TRawMessage> rawMessages)
        {
            var packet = CreateOutgoingPacket();
            var packetLength = 0;
            foreach (var rawMessage in rawMessages)
            {
                if (packetLength + rawMessage.Length > _connection.MTU)
                {
                    yield return packet;

                    packet = CreateOutgoingPacket();
                    packetLength = 0;
                }
                packet.RawMessages.Add(rawMessage);
                packetLength += rawMessage.Length;
            }

            if (packetLength > 0)
            {
                yield return packet;
            }
        }
    }
}
