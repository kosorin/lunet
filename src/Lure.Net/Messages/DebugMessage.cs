using Lure.Net.Data;
using System;

namespace Lure.Net.Messages
{
    [Obsolete]
    [NetMessage(0)]
    public class DebugMessage : NetMessage
    {
        public int Integer { get; set; }

        public float Float { get; set; }

        public override string ToString()
        {
            return $"Int: {Integer}; Float: {Float}";
        }

        protected override void Deserialize(NetDataReader reader)
        {
            Integer = reader.ReadInt();
            Float = reader.ReadFloat();
        }

        protected override void Serialize(NetDataWriter writer)
        {
            writer.WriteInt(Integer);
            writer.WriteFloat(Float);
        }
    }
}
