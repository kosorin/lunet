using Lure.Net.Packets.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lure.Net.Channels
{
    internal interface IMessageChannel : INetChannel
    {
        IEnumerable<RawMessage> GetReceivedRawMessages();
    }
}
