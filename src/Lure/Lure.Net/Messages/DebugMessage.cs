using Lure.Net.Data;

namespace Lure.Net.Messages
{
    [NetMessage(0)]
    public class DebugMessage : NetMessage
    {
        public int Integer { get; set; }

        public float Float { get; set; }

        public override string ToString()
        {
            return $"Int: {Integer}; Float: {Float}";
        }

        protected override void Deserialize(INetDataReader reader)
        {
            Integer = reader.ReadInt();
            Float = reader.ReadFloat();
        }

        protected override void Serialize(INetDataWriter writer)
        {
            writer.WriteInt(Integer);
            writer.WriteFloat(Float);
        }
    }
}
