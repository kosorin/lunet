using Lure.Net.Data;
using System;

namespace Lure.Net.Messages
{
    [Obsolete]
    [NetMessage(0)]
    public class DebugMessage : NetMessage
    {
        public int Id { get; set; }

        public override string ToString()
        {
            return $"ID {Id,6}";
        }

        protected override void Deserialize(NetDataReader reader)
        {
            Id = reader.ReadInt();
        }

        protected override void Serialize(NetDataWriter writer)
        {
            writer.WriteInt(Id);
        }
    }
}
