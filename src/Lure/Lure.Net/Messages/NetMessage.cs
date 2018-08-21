using Lure.Net.Data;

namespace Lure.Net.Messages
{
    public abstract class NetMessage
    {
        internal ushort TypeId { get; set; }

        internal void DeserializeLib(INetDataReader reader)
        {
            // Skip reading a type id - already read and used to create a message

            Deserialize(reader);
        }

        internal void SerializeLib(INetDataWriter writer)
        {
            writer.WriteUShort(TypeId);

            Serialize(writer);
        }

        protected abstract void Deserialize(INetDataReader reader);

        protected abstract void Serialize(INetDataWriter writer);
    }
}
