using Lure.Net.Data;
using Lure.Net.Extensions;
using System.Diagnostics;

namespace Lure.Net.Packets
{
    public class RawMessage
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
