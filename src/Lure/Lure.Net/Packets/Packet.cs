using Lure.Net.Data;
using System;

namespace Lure.Net.Packets
{
    internal abstract class Packet
    {
        public byte ChannelId { get; set; }

        public PacketDirection Direction { get; set; }

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

        protected abstract void DeserializeDataCore(INetDataReader reader);

        protected abstract void SerializeHeaderCore(INetDataWriter writer);

        protected abstract void SerializeDataCore(INetDataWriter writer);
    }
}
