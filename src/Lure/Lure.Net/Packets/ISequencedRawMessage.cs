namespace Lure.Net.Packets
{
    internal interface ISequencedRawMessage
    {
        SeqNo Seq { get; set; }
    }
}