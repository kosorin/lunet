using Lure.Net.Data;

namespace Lure.Net.Messages
{
    public abstract class NetMessage
    {
        internal ushort TypeId { get; set; }

        internal void Deserialize(INetDataReader reader)
        {
            // Skip reading a type - already read and used to create a message

            DeserializeCore(reader);
        }

        internal void Serialize(INetDataWriter writer)
        {
            writer.WriteUShort(TypeId);

            SerializeCore(writer);
        }

        protected abstract void DeserializeCore(INetDataReader reader);

        protected abstract void SerializeCore(INetDataWriter writer);
    }
}
