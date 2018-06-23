using Lure.Net.Data;

namespace Lure.Net.Packets
{
    internal interface IPacketPart
    {
        /// <summary>
        /// Gets the length in bytes.
        /// </summary>
        int Length { get; }

        void Deserialize(INetDataReader reader);

        void Serialize(INetDataWriter writer);
    }
}
