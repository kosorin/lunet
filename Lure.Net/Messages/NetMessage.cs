using System;
using Lure.Net.Data;

namespace Lure.Net.Messages
{
    public abstract class NetMessage : INetSerializable
    {
        internal ushort TypeId { get; set; }

        public void Deserialize(INetDataReader reader)
        {
            TypeId = reader.ReadUShort();

            DeserializeCore(reader);
        }

        public void Serialize(INetDataWriter writer)
        {
            writer.WriteUShort(TypeId);

            SerializeCore(writer);
        }

        protected abstract void DeserializeCore(INetDataReader reader);

        protected abstract void SerializeCore(INetDataWriter writer);
    }
}
