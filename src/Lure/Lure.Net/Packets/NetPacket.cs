using Lure.Net.Data;
using System;
using System.Collections.Generic;

namespace Lure.Net.Packets
{
    public abstract class NetPacket<TRawMessage> : INetPacket
        where TRawMessage : RawMessage
    {
        private readonly Func<TRawMessage> _rawMessageActivator;

        protected NetPacket(Func<TRawMessage> rawMessageActivator)
        {
            _rawMessageActivator = rawMessageActivator;
        }

        public List<TRawMessage> RawMessages { get; } = new List<TRawMessage>();

        public void DeserializeHeader(NetDataReader reader)
        {
            try
            {
                DeserializeHeaderCore(reader);
                reader.PadBits();
            }
            catch (Exception e)
            {
                throw new NetSerializationException("Could not deserialize packet header.", e);
            }
        }

        public void DeserializeData(NetDataReader reader)
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

        public void SerializeHeader(NetDataWriter writer)
        {
            try
            {
                SerializeHeaderCore(writer);
                writer.PadBits();
            }
            catch (Exception e)
            {
                throw new NetSerializationException("Could not serialize packet header.", e);
            }
        }

        public void SerializeData(NetDataWriter writer)
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

        protected virtual void DeserializeHeaderCore(NetDataReader reader)
        {
        }

        protected virtual void DeserializeDataCore(NetDataReader reader)
        {
            RawMessages.Clear();
            while (reader.Position < reader.Length)
            {
                var rawMessage = _rawMessageActivator();
                rawMessage.Deserialize(reader);
                RawMessages.Add(rawMessage);
            }
        }

        protected virtual void SerializeHeaderCore(NetDataWriter writer)
        {
        }

        protected virtual void SerializeDataCore(NetDataWriter writer)
        {
            foreach (var rawMessage in RawMessages)
            {
                rawMessage.Serialize(writer);
            }
        }
    }
}
