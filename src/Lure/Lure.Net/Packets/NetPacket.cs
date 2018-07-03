using Lure.Collections;
using Lure.Net.Data;
using System;
using System.Collections.Generic;

namespace Lure.Net.Packets
{
    internal abstract class NetPacket<TRawMessage> : INetPacket
        where TRawMessage : RawMessage
    {
        protected readonly ObjectPool<TRawMessage> _rawMessagePool;

        protected NetPacket(ObjectPool<TRawMessage> rawMessagePool)
        {
            _rawMessagePool = rawMessagePool;
        }

        public byte ChannelId { get; set; }

        public List<TRawMessage> RawMessages { get; set; } = new List<TRawMessage>();

        public NetPacketDirection Direction { get; set; }

        private static int SerializationCheck => 0x55555555;

        public void DeserializeHeader(INetDataReader reader)
        {
            try
            {
                // Skip reading a channel id - already read and used to create a channel

                DeserializeHeaderCore(reader);

                reader.PadBits();
                if (reader.ReadInt() != SerializationCheck)
                {
                    // TODO: Handle bad packets
                    throw new NetSerializationException("Wrong packet serialization check.");
                }
            }
            catch (Exception e)
            {
                throw new NetSerializationException("Could not deserialize packet header.", e);
            }
        }

        public void DeserializeData(INetDataReader reader)
        {
            try
            {
                DeserializeDataCore(reader);

                if (reader.Position != reader.Length)
                {
                    // TODO: Handle bad packets
                    throw new NetSerializationException("Remaining data in a packet.");
                }
            }
            catch (Exception e)
            {
                throw new NetSerializationException("Could not deserialize packet data.", e);
            }
        }

        public void SerializeHeader(INetDataWriter writer)
        {
            try
            {
                writer.WriteByte(ChannelId);

                SerializeHeaderCore(writer);

                writer.PadBits();
                writer.WriteInt(SerializationCheck);
            }
            catch (Exception e)
            {
                throw new NetSerializationException("Could not serialize packet header.", e);
            }
        }

        public void SerializeData(INetDataWriter writer)
        {
            try
            {
                SerializeDataCore(writer);
            }
            catch (Exception e)
            {
                throw new NetSerializationException("Could not serialize packet data.", e);
            }
        }

        protected abstract void DeserializeHeaderCore(INetDataReader reader);

        protected virtual void DeserializeDataCore(INetDataReader reader)
        {
            RawMessages.Clear();
            while (reader.Position < reader.Length)
            {
                var rawMessage = _rawMessagePool.Rent();
                rawMessage.Deserialize(reader);
                RawMessages.Add(rawMessage);
            }
        }

        protected abstract void SerializeHeaderCore(INetDataWriter writer);

        protected virtual void SerializeDataCore(INetDataWriter writer)
        {
            foreach (var rawMessage in RawMessages)
            {
                rawMessage.Serialize(writer);
            }
        }
    }
}
