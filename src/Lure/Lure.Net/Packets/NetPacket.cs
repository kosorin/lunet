using Lure.Collections;
using Lure.Net.Data;
using System;
using System.Collections.Generic;

namespace Lure.Net.Packets
{
    public abstract class NetPacket<TRawMessage> : INetPacket
        where TRawMessage : RawMessage
    {
        private static int SerializationCheck => 0x55555555;

        private readonly Func<TRawMessage> _rawMessageActivator;

        protected NetPacket(Func<TRawMessage> rawMessageActivator)
        {
            _rawMessageActivator = rawMessageActivator;
        }

        public List<TRawMessage> RawMessages { get; set; } = new List<TRawMessage>();

        public void DeserializeHeader(INetDataReader reader)
        {
            try
            {
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

        protected virtual void DeserializeHeaderCore(INetDataReader reader)
        {
        }

        protected virtual void DeserializeDataCore(INetDataReader reader)
        {
            RawMessages.Clear();
            while (reader.Position < reader.Length)
            {
                var rawMessage = _rawMessageActivator();
                rawMessage.Deserialize(reader);
                RawMessages.Add(rawMessage);
            }
        }

        protected virtual void SerializeHeaderCore(INetDataWriter writer)
        {
        }

        protected virtual void SerializeDataCore(INetDataWriter writer)
        {
            foreach (var rawMessage in RawMessages)
            {
                rawMessage.Serialize(writer);
            }
        }
    }
}
