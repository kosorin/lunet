using Lure.Net.Data;
using Lure.Net.Extensions;

namespace Lure.Net.Packets.Message
{
    internal abstract class RawMessage : IPacketPart
    {
        public byte[] Data { get; set; }

        public long? Timestamp { get; set; }

        public virtual int Length => sizeof(ushort) + Data.Length;

        public virtual void Deserialize(INetDataReader reader)
        {
            Data = reader.ReadByteArray();
        }

        public virtual void Serialize(INetDataWriter writer)
        {
            writer.WriteByteArray(Data);
        }
    }
}
