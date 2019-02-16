namespace Lure.Net.Data
{
    public interface INetData
    {
        void Deserialize(NetDataReader reader);

        void Serialize(NetDataWriter writer);
    }
}
