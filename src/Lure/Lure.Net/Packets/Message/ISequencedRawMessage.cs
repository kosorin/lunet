namespace Lure.Net.Packets.Message
{
    internal interface ISequencedRawMessage
    {
        SeqNo Seq { get; set; }
    }
}