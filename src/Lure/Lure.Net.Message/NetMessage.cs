using Lure.Net.Data;
using System;

namespace Lure.Net.Message
{
    [Obsolete]
    public abstract class NetMessage
    {
        internal ushort TypeId { get; set; }

        public void DeserializeLib(NetDataReader reader)
        {
            // Skip reading a type id - already read and used to create a message

            Deserialize(reader);
        }

        public void SerializeLib(NetDataWriter writer)
        {
            writer.WriteUShort(TypeId);

            Serialize(writer);
        }

        protected abstract void Deserialize(NetDataReader reader);

        protected abstract void Serialize(NetDataWriter writer);
    }
}
