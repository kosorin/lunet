namespace Lure.Net.Data
{
    public interface INetSerializable
    {
        void Deserialize(NetDataReader reader);

        void Serialize(NetDataWriter writer);
    }
}
