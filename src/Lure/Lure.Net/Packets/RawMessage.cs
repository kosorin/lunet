using Lure.Net.Data;
using Lure.Net.Extensions;

namespace Lure.Net.Packets
{
    internal class RawMessage : IRawMessage
    {
        public long Timestamp { get; set; }

        public byte[] Data { get; set; }

        public int Length => sizeof(ushort) + Data.Length;

        public void Deserialize(INetDataReader reader)
        {
            Data = reader.ReadByteArray();
        }

        public void Serialize(INetDataWriter writer)
        {
            writer.WriteByteArray(Data);
        }
    }
}
