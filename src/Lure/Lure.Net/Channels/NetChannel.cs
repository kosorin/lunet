using Lure.Collections;
using Lure.Net.Data;
using Lure.Net.Packets;
using System.Collections.Generic;

namespace Lure.Net.Channels
{
    internal abstract class NetChannel<TPacket, TRawMessage> : INetChannel
        where TPacket : NetPacket<TRawMessage>
        where TRawMessage : RawMessage
    {
        protected readonly byte _id;
        protected readonly NetConnection _connection;

        protected readonly ObjectPool<TPacket> _packetPool;
        protected readonly ObjectPool<TRawMessage> _rawMessagePool;

        private bool _disposed;

        protected NetChannel(byte id, NetConnection connection)
        {
            _id = id;
            _connection = connection;

            var packetActivator = ObjectActivatorFactory.CreateParameterized<ObjectPool<TRawMessage>, TPacket>();
            _packetPool = new ObjectPool<TPacket>(() => packetActivator(_rawMessagePool));
            _rawMessagePool = new ObjectPool<TRawMessage>();
        }

        public byte Id => _id;


        public void Dispose()
        {
            Dispose(true);
        }

        public virtual void Update()
        {
            var outgoingRawMessages = GetOutgoingRawMessages();
            var outgoingPackets = PackOutgoingRawMessages(outgoingRawMessages);
            foreach (var packet in outgoingPackets)
            {
                SendPacket(packet);
            }
        }

        public virtual void ReceivePacket(INetDataReader reader)
        {
            var packet = _packetPool.Rent();

            try
            {
                packet.DeserializeHeader(reader);
            }
            catch (NetSerializationException)
            {
                _packetPool.Return(packet);
                return;
            }

            if (!AcceptIncomingPacket(packet))
            {
                return;
            }

            try
            {
                packet.DeserializeData(reader);
            }
            catch (NetSerializationException)
            {
                foreach (var rawMessage in packet.RawMessages)
                {
                    _rawMessagePool.Return(rawMessage);
                }
                packet.RawMessages.Clear();
                _packetPool.Return(packet);
                return;
            }

            OnIncomingPacket(packet);

            var now = Timestamp.Current;
            foreach (var rawMessage in packet.RawMessages)
            {
                rawMessage.Timestamp = now;
                if (AcceptIncomingRawMessage(rawMessage))
                {
                    OnIncomingRawMessage(rawMessage);
                }
                else
                {
                    _rawMessagePool.Return(rawMessage);
                }
            }

            _packetPool.Return(packet);
        }

        public abstract IEnumerable<RawMessage> GetReceivedRawMessages();

        public void SendMessage(byte[] data)
        {
            var rawMessage = CreateOutgoingRawMessage(data);
            OnOutgoingRawMessage(rawMessage);
        }


        protected abstract bool AcceptIncomingPacket(TPacket packet);

        protected abstract bool AcceptIncomingRawMessage(TRawMessage rawMessage);

        protected abstract void OnIncomingPacket(TPacket packet);

        protected abstract void OnIncomingRawMessage(TRawMessage rawMessage);


        protected abstract List<TRawMessage> GetOutgoingRawMessages();

        protected TPacket CreateOutgoingPacket()
        {
            var packet = _packetPool.Rent();
            packet.Direction = NetPacketDirection.Outgoing;
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

            _packetPool.Return(packet);
        }

        protected virtual void Dispose(bool disposing)
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
