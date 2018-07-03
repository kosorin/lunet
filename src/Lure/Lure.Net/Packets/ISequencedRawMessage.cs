using System;

namespace Lure.Net.Packets
{
    internal interface ISequencedRawMessage : IComparable<ISequencedRawMessage>
    {
        SeqNo Seq { get; set; }
    }
}