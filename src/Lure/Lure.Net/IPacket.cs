using Lure.Net.Data;

namespace Lure.Net
{
    public interface IPacket
    {
        void DeserializeHeader(NetDataReader reader);

        void DeserializeData(NetDataReader reader);

        void SerializeHeader(NetDataWriter writer);

        void SerializeData(NetDataWriter writer);
    }
}