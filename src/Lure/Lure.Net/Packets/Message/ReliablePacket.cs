﻿using Lure.Collections;
using Lure.Net.Data;
using Lure.Net.Extensions;
using System.Collections.Generic;
using System.Diagnostics;

namespace Lure.Net.Packets.Message
{
    internal class ReliablePacket : MessagePacket<ReliableRawMessage>, IPoolable
    {
        public ReliablePacket(ObjectPool<ReliableRawMessage> rawMessagePool) : base(rawMessagePool)
        {
        }

        public static int AckBufferLength { get; } = 8;// 64;

        public static int PacketAckBufferLength { get; } = 4;// sizeof(uint) * NC.BitsPerByte;

        public SeqNo Seq { get; set; }

        public SeqNo Ack { get; set; }

        public BitVector AckBuffer { get; set; }

        void IPoolable.OnRent()
        {
        }

        void IPoolable.OnReturn()
        {
            if (Direction == PacketDirection.Outgoing)
            {
                // Outgoing raw messages are saved in a channel and waiting for an ack
            }
            else
            {
                foreach (var rawMessage in RawMessages)
                {
                    _rawMessagePool.Return(rawMessage);
                }
            }
            RawMessages.Clear();
        }

        protected override void DeserializeHeaderCore(INetDataReader reader)
        {
            Seq = reader.ReadSeqNo();
            Ack = reader.ReadSeqNo();
            AckBuffer = reader.ReadBits(PacketAckBufferLength);
        }

        protected override void DeserializeDataCore(INetDataReader reader)
        {
            base.DeserializeDataCore(reader);

            RawMessages.Sort();
        }

        protected override void SerializeHeaderCore(INetDataWriter writer)
        {
            writer.WriteSeqNo(Seq);
            writer.WriteSeqNo(Ack);
            writer.WriteBits(AckBuffer);
        }
    }
}
