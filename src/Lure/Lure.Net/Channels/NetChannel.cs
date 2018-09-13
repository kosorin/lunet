using Lure.Collections;
using Lure.Net.Data;
using Lure.Net.Packets;
using System.Collections.Generic;
using System.Linq;

namespace Lure.Net.Channels
{
    internal abstract class NetChannel<TPacket, TRawMessage> : INetChannel
        where TPacket : NetPacket<TRawMessage>
        where TRawMessage : RawMessage
    {
        protected readonly NetConnection _connection;

        protected readonly IObjectPool<TPacket> _packetPool;
        protected readonly IObjectPool<TRawMessage> _rawMessagePool;

        protected NetChannel(NetConnection connection)
        {
            _connection = connection;

            var packetActivator = ObjectActivatorFactory.CreateParameterized<IObjectPool<TRawMessage>, TPacket>();
            _packetPool = new ObjectPool<TPacket>(() => packetActivator(_rawMessagePool));
            _rawMessagePool = new ObjectPool<TRawMessage>();
        }

        public void ProcessIncomingPacket(INetDataReader reader)
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

        public IList<INetPacket> CollectOutgoingPackets()
        {
            var outgoingRawMessages = GetOutgoingRawMessages();
            var outgoingPackets = PackOutgoingRawMessages(outgoingRawMessages);
            foreach (var packet in outgoingPackets)
            {
                OnOutgoingPacket(packet);

                var now = Timestamp.Current;
                foreach (var rawMessage in packet.RawMessages)
                {
                    rawMessage.Timestamp = now;
                }

#error Pool Packet
                _packetPool.Return(packet);
            }
            return outgoingPackets.Cast<INetPacket>().ToList();
        }

        public abstract IList<byte[]> GetReceivedMessages();

        public void SendMessage(byte[] data)
        {
            var rawMessage = CreateOutgoingRawMessage(data);
            OnOutgoingRawMessage(rawMessage);
        }


        protected abstract bool AcceptIncomingPacket(TPacket packet);

        protected abstract bool AcceptIncomingRawMessage(TRawMessage rawMessage);

        protected abstract void OnIncomingPacket(TPacket packet);

        protected abstract void OnIncomingRawMessage(TRawMessage rawMessage);


        protected virtual IList<TPacket> PackOutgoingRawMessages(List<TRawMessage> rawMessages)
        {
            var packets = new List<TPacket>();

            var packet = CreateOutgoingPacket();
            var packetLength = 0;
            foreach (var rawMessage in rawMessages)
            {
                if (packetLength + rawMessage.Length > _connection.MTU)
                {
                    packets.Add(packet);

                    packet = CreateOutgoingPacket();
                    packetLength = 0;
                }
                packet.RawMessages.Add(rawMessage);
                packetLength += rawMessage.Length;
            }

            if (packetLength > 0)
            {
                packets.Add(packet);
            }

            return packets;
        }

        protected TPacket CreateOutgoingPacket()
        {
            var packet = _packetPool.Rent();
            packet.Direction = NetPacketDirection.Outgoing;

            PrepareOutgoingPacket(packet);

            return packet;
        }

        protected abstract List<TRawMessage> GetOutgoingRawMessages();

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


        private bool _disposed;

        public void Dispose()
        {
            Dispose(true);
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
    }
}
