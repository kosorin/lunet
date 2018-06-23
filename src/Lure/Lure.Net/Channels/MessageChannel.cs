using Lure.Collections;
using Lure.Net.Data;
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

        public abstract void SendRawMessage(byte[] data);

        public sealed override void ReceivePacket(NetDataReader reader)
        {
            var packet = _packetPool.Rent();

            packet.DeserializeHeader(reader);

            if (!AcceptIncomingPacket(packet))
            {
                return;
            }

            packet.DeserializeData(reader);

            Log.Verbose("[{RemoteEndPoint}] Message <<<", _connection.RemoteEndPoint);
            OnIncomingPacket(packet);
            LastIncomingPacketTimestamp = Timestamp.Current;

            ParseRawMessages(packet);

            _packetPool.Return(packet);
        }

        public override void Update()
        {
            var outgoingRawMessages = CollectOutgoingRawMessages();

            var packet = CreateOutgoingPacket();
            var packetLength = 0;
            foreach (var rawMessage in outgoingRawMessages)
            {
                if (packetLength + rawMessage.Length > _connection.MTU)
                {
                    SendPacket(packet);

                    packet = CreateOutgoingPacket();
                    packetLength = 0;
                }
                packet.RawMessages.Add(rawMessage);
                packetLength += rawMessage.Length;
            }

            if (packetLength > 0)
            {
                SendPacket(packet);
            }
        }

        protected abstract bool AcceptIncomingPacket(TPacket packet);

        protected abstract void PrepareOutgoingPacket(TPacket packet);

        protected abstract List<TRawMessage> CollectOutgoingRawMessages();

        protected abstract void ParseRawMessages(TPacket packet);

        protected TPacket CreateOutgoingPacket()
        {
            var packet = _packetPool.Rent();
            packet.Direction = PacketDirection.Outgoing;
            packet.ChannelId = _id;

            PrepareOutgoingPacket(packet);

            return packet;
        }

        protected void SendPacket(TPacket packet)
        {
            _connection.Peer.SendPacket(_connection, packet);

            Log.Verbose("[{RemoteEndPoint}] Message >>>", _connection.RemoteEndPoint);
            OnOutgoingPacket(packet);
            LastOutgoingPacketTimestamp = Timestamp.Current;

            _packetPool.Return(packet);
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

        protected virtual void OnIncomingPacket(TPacket packet)
        {
            var now = Timestamp.Current;
            foreach (var rawMessage in packet.RawMessages)
            {
                rawMessage.Timestamp = now;
            }
        }

        protected virtual void OnOutgoingPacket(TPacket packet)
        {
            var now = Timestamp.Current;
            foreach (var rawMessage in packet.RawMessages)
            {
                rawMessage.Timestamp = now;
            }
        }
    }
}
