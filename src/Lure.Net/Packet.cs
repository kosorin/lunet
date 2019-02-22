using Lure.Net.Data;

namespace Lure.Net
{
    public abstract class Packet : IPacket
    {
        public int Length => HeaderLength + DataLength;

        public abstract int HeaderLength { get; }

        public abstract int DataLength { get; }

        public abstract void DeserializeHeader(NetDataReader reader);

        public abstract void DeserializeData(NetDataReader reader);

        public abstract void SerializeHeader(NetDataWriter writer);

        public abstract void SerializeData(NetDataWriter writer);
    }
}
