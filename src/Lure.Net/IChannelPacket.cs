using Lunet.Data;

namespace Lunet
{
    public interface IChannelPacket
    {
        int Length { get; }

        void DeserializeHeader(NetDataReader reader);

        void DeserializeData(NetDataReader reader);

        void SerializeHeader(NetDataWriter writer);

        void SerializeData(NetDataWriter writer);
    }
}