namespace Lure.Net.Packets
{
    internal interface IRawMessage : IPacketPart
    {
        long Timestamp { get; set; }

        byte[] Data { get; set; }
    }
}
