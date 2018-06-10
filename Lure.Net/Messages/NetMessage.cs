using System;
using Lure.Net.Data;

namespace Lure.Net.Messages
{
    public abstract class NetMessage : INetSerializable
    {
        internal ushort TypeId { get; set; }

        void INetSerializable.Deserialize(INetDataReader reader)
        {
            // Skip reading a type - already read

            DeserializeCore(reader);
        }

        void INetSerializable.Serialize(INetDataWriter writer)
        {
            writer.WriteUShort(TypeId);

            SerializeCore(writer);
        }

        protected abstract void DeserializeCore(INetDataReader reader);

        protected abstract void SerializeCore(INetDataWriter writer);
    }
}
