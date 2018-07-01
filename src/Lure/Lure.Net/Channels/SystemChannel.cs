using Lure.Collections;
using Lure.Net.Data;
using Lure.Net.Messages;
using Lure.Net.Packets;
using Lure.Net.Packets.Message;
using Lure.Net.Packets.System;
using System.Collections.Generic;

namespace Lure.Net.Channels
{
    internal class SystemChannel : NetChannel
    {
        public const byte DefaultId = 0;

        private readonly SystemPacketPool _packetPool;

        private const int ResendTimeout = 100;

        private readonly Queue<SystemPacket> _outgoingPacketQueue = new Queue<SystemPacket>();
        private readonly Dictionary<SeqNo, SystemPacket> _pendingPacketQueue = new Dictionary<SeqNo, SystemPacket>();
        private SeqNo _outgoingPacketSeq = SeqNo.Zero;

        private readonly Dictionary<SeqNo, SystemPacket> _incomingPacketQueue = new Dictionary<SeqNo, SystemPacket>();
        private SeqNo _incomingPacketReadSeq = SeqNo.Zero - 1;
        private SeqNo _incomingPacketExpectedSeq = SeqNo.Zero;

        private SeqNo _incomingPacketAck = SeqNo.Zero - 1;
        private BitVector _incomingPacketAckBuffer = new BitVector(ReliablePacket.ChannelAckBufferLength);

        private bool _requireAcknowledgement;

        private bool _disposed;

        public SystemChannel(NetConnection connection) : base(DefaultId, connection)
        {
            _packetPool = new SystemPacketPool();
        }

        public override void Update()
        {
            lock (_outgoingPacketQueue)
            {
                while (_outgoingPacketQueue.Count > 0)
                {
                    var packet = _outgoingPacketQueue.Dequeue();
                    PrepareOutgoingPacket(packet);
                    _pendingPacketQueue[packet.Seq] = packet;
                }
            }

            var outgoingPackets = PackOutgoingRawMessages(outgoingRawMessages);
            foreach (var packet in outgoingPackets)
            {
                SendPacket(packet);
            }
        }

        public override void ReceivePacket(NetDataReader reader)
        {
            var type = (SystemPacketType)reader.ReadByte();

            var packet = _packetPool.Rent(type);

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
            LastIncomingPacketTimestamp = now;

            _packetPool.Return(packet);
        }

        public void SendPacket(SystemPacket packet)
        {
            lock (_outgoingPacketQueue)
            {
                _outgoingPacketQueue.Enqueue(packet);
            }
        }

        public TSystemPacket CreateOutgoingPacket<TSystemPacket>() where TSystemPacket : SystemPacket
        {
            var packet = _packetPool.Rent<TSystemPacket>();
            packet.Timestamp = null;
            packet.Direction = PacketDirection.Outgoing;
            packet.ChannelId = _id;

            return packet;
        }

        private void PrepareOutgoingPacket(SystemPacket packet)
        {

        }

        protected bool AcceptIncomingPacket(TPacket packet)
        {

        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _packetPool.Dispose();
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
