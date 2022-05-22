using Lunet.Data;

namespace Lunet;

public abstract class ChannelPacket
{
    public int Length => HeaderLength + DataLength;

    public virtual int HeaderLength => sizeof(byte) + sizeof(byte); // PacketType + ChannelId

    public abstract int DataLength { get; }

    public abstract void DeserializeHeader(NetDataReader reader);

    public abstract void DeserializeData(NetDataReader reader);

    public abstract void SerializeHeader(NetDataWriter writer);

    public abstract void SerializeData(NetDataWriter writer);
}
