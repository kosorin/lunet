using Lure.Net.Data;
using Lure.Net.Extensions;

namespace Lure.Net.Channels.Message
{
    public class Message
    {
        public byte[] Data { get; set; }

        public long? Timestamp { get; set; }

        public virtual int Length => sizeof(ushort) + Data.Length;

        public virtual void Deserialize(NetDataReader reader)
        {
            Data = reader.ReadByteArray();
        }

        public virtual void Serialize(NetDataWriter writer)
        {
            writer.WriteByteArray(Data);
        }
    }
}
