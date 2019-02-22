using Lure.Net.Data;

namespace Lure.Net
{
    public interface IPacket
    {
        int Length { get; }

        void DeserializeHeader(NetDataReader reader);

        void DeserializeData(NetDataReader reader);

        void SerializeHeader(NetDataWriter writer);

        void SerializeData(NetDataWriter writer);
    }
}