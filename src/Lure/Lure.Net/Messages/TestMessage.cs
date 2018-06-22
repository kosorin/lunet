using Lure.Net.Data;

namespace Lure.Net.Messages
{
    [NetMessage(SystemMessageType.Test)]
    public class TestMessage : NetMessage
    {
        public int Integer { get; set; }

        public float Float { get; set; }

        public override string ToString()
        {
            return $"Int: {Integer}; Float: {Float}";
        }

        protected override void DeserializeCore(INetDataReader reader)
        {
            Integer = reader.ReadInt();
            Float = reader.ReadFloat();
        }

        protected override void SerializeCore(INetDataWriter writer)
        {
            writer.WriteInt(Integer);
            writer.WriteFloat(Float);
        }
    }
}
