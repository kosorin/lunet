using Lure.Net.Data;
using Lure.Net.Extensions;
using System.Diagnostics;

namespace Lure.Net.Packets
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class RawMessage
    {
        public byte[] Data { get; set; }

        public long? Timestamp { get; set; }

        public virtual int Length => sizeof(ushort) + Data.Length;

        protected virtual string DebuggerDisplay => $"Length = {Length}, Timestamp = {Timestamp ?? -1}";

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
