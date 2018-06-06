namespace Lure.Net
{
    public abstract class NetMessage
    {
        public ushort Id { get; internal set; }

        public abstract void Deserialize(NetDataReader reader);

        public abstract void Serialize(NetDataWriter writer);
    }

    public class TestMessage : NetMessage
    {
        public int Integer { get; set; }

        public float Float { get; set; }

        public override void Deserialize(NetDataReader reader)
        {
            Integer = reader.ReadInt();
            Float = reader.ReadFloat();
        }

        public override void Serialize(NetDataWriter writer)
        {
            writer.WriteInt(Integer);
            writer.WriteFloat(Float);
        }
    }
}
