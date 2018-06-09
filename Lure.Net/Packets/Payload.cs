using System;
using System.Collections.Generic;
using System.Linq;

namespace Lure.Net.Packets
{
    internal class Payload
    {
        public List<PayloadMessage> Messages { get; } = new List<PayloadMessage>();

        public int TotalLength => Messages.Sum(x => x.Data.Length);
    }
}
