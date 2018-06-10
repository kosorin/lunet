namespace Lure.Net.Data
{
    public interface INetSerializable
    {
        void Deserialize(INetDataReader reader);

        void Serialize(INetDataWriter writer);
    }
}
