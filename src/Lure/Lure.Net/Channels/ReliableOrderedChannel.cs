using Lure.Extensions.NetCore;
using Lure.Net.Data;
using Lure.Net.Extensions;
using Lure.Net.Messages;
using Lure.Net.Packets;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace Lure.Net.Channels
{
    internal class ReliableOrderedChannel : NetChannel<ReliablePacket>
    {
        private const int MTU = 1000;
        private const int ResendTimeout = 100;
        private const int KeepAliveTimeout = 1000;

        private static readonly ILogger Logger = Log.ForContext<ReliableOrderedChannel>();

        private readonly ObjectPool<NetDataWriter> _writerPool;

        private readonly Dictionary<SeqNo, PayloadMessage> _sendQueue = new Dictionary<SeqNo, PayloadMessage>();
        private readonly Dictionary<SeqNo, PacketData> _sentPayloads = new Dictionary<SeqNo, PacketData>();
        private SeqNo _sendMessageSeq = SeqNo.Zero;
        private SeqNo _sendPacketSeq = SeqNo.Zero;

        private SeqNo _receivePacketAck = SeqNo.Zero - 1;
        private BitVector _receivePacketAckBuffer = new BitVector(ReliablePacket.AckBufferLength);

        private bool _disposed;

        public ReliableOrderedChannel(byte id, NetConnection connection) : base(id, connection)
        {
            _writerPool = new ObjectPool<NetDataWriter>(16, () => new NetDataWriter(MTU));
        }


        public void SendMessage(NetMessage message)
        {
            var data = SerializeMessage(message);

            lock (_sendQueue)
            {
                var payloadMessage = new PayloadMessage
                {
                    Seq = _sendMessageSeq++,
                    Data = data,
                };
                if (!_sendQueue.TryAdd(payloadMessage.Seq, payloadMessage))
                {
                    throw new NetException("Buffer overflow.");
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _writerPool.Dispose();
                }
                _disposed = true;
            }
            base.Dispose(disposing);
        }

        protected override void OnPacketReceived(ReliablePacket packet)
        {
            if (AckReceive(packet.Seq))
            {
                AckSend(packet.Ack, packet.AckBuffer);
            }
        }

        protected override void PrepareOutgoingPacket(ReliablePacket packet)
        {
            packet.Seq = _sendPacketSeq++;
            packet.Ack = _receivePacketAck;
            packet.AckBuffer = _receivePacketAckBuffer.Clone(0, ReliablePacket.PacketAckBufferLength);
        }

        /// <summary>
        /// Acks received packet.
        /// </summary>
        /// <param name="seq"></param>
        /// <returns>Returns <c>true</c> if <paramref name="seq"/> wasn't acked yet.</returns>
        internal bool AckReceive(SeqNo seq)
        {
            var diff = seq.GetDifference(_receivePacketAck);
            if (diff == 0)
            {
                return false;
            }
            else if (diff > 0)
            {
                _receivePacketAck = seq;

                if (diff > _receivePacketAckBuffer.Capacity)
                {
                    _receivePacketAckBuffer.ClearAll();
                }
                else
                {
                    _receivePacketAckBuffer.LeftShift(diff);
                    _receivePacketAckBuffer.Set(diff - 1);
                }
                goto Success;
            }
            else
            {
                diff *= -1;
                if (diff <= _receivePacketAckBuffer.Capacity)
                {
                    var ackIndex = diff - 1;
                    if (_receivePacketAckBuffer[ackIndex])
                    {
                        return false;
                    }
                    else
                    {
                        _receivePacketAckBuffer.Set(diff - 1);
                        goto Success;
                    }
                }
                return false;
            }

            Success:
            Logger.Verbose("  {Acks} <- {Ack}", _receivePacketAckBuffer, _receivePacketAck.Value);
            return true;
        }

        internal void AckSend(SeqNo ack, BitVector acks)
        {
            lock (_sendQueue)
            {
                AckSend(ack);
                foreach (var bit in acks.AsBits())
                {
                    ack--;
                    if (bit)
                    {
                        AckSend(ack);
                    }
                }
            }
        }

        internal List<PacketData> GetQueuedPayloads()
        {
            var dataList = new List<PacketData>();

            List<PayloadMessage> payloadMessages;
            lock (_sendQueue)
            {
                if (_sendQueue.Count == 0)
                {
                    return dataList;
                }
                payloadMessages = _sendQueue.Values
                    .Where(x => x.LastSendTimestamp == null || Timestamp.Current - x.LastSendTimestamp > ResendTimeout)
                    .OrderBy(x => x.LastSendTimestamp ?? long.MaxValue)
                    .ToList();
            }

            foreach (var payloadMessage in payloadMessages)
            {
                payloadMessage.LastSendTimestamp = Timestamp.Current;
            }

            var data = new PayloadPacketData();
            foreach (var payloadMessage in payloadMessages)
            {
                if (payloadMessage.Length > MTU)
                {
                    throw new NetException();
                }
                else if (data.Length + payloadMessage.Length > MTU)
                {
                    dataList.Add(data);
                    data = new PayloadPacketData();
                }

                data.Messages.Add(payloadMessage);
            }

            if (data.Messages.Count > 0)
            {
                dataList.Add(data);
            }

            return dataList;
        }

        private void AckSend(SeqNo ack)
        {
            if (_sentPayloads.Remove(ack, out var data))
            {
                foreach (var message in data.Messages)
                {
                    _sendQueue.Remove(message.Seq);
                }
            }
        }

        private void SendPacket(ReliablePacket packet)
        {
            _connection.Peer.SendPacket(_connection, packet);
            _packetPool.Return(packet);
            _lastPacketTimestamp = Timestamp.Current;
        }

        private byte[] SerializeMessage(NetMessage message)
        {
            var writer = _writerPool.Rent();
            try
            {
                writer.Reset();
                writer.WriteSerializable(message);
                writer.Flush();

                return writer.GetBytes();
            }
            finally
            {
                _writerPool.Return(writer);
            }
        }

        private byte[] SerializePayload(Payload payload)
        {
            var writer = _writerPool.Rent();
            try
            {
                writer.Reset();
                foreach (var payloadMessage in payload.Messages)
                {
                    writer.WriteSeqNo(payloadMessage.Seq);
                    writer.WriteBytes(payloadMessage.Data);
                    writer.PadBits();
                }
                writer.Flush();

                return writer.GetBytes();
            }
            finally
            {
                _writerPool.Return(writer);
            }
        }

    }

}
