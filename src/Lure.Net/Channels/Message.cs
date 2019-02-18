using Lure.Net.Data;
using Lure.Net.Extensions;

namespace Lure.Net.Channels
{
    public abstract class Message
    {
        public byte[] Data { get; set; }

        public int Length => HeaderLength + DataLength;

        public virtual int HeaderLength => 0;

        public virtual int DataLength => sizeof(ushort) + Data.Length;

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
