using System.Collections.Generic;

namespace Lure.Net
{
    public interface IChannelFactory
    {
        IEnumerable<IChannel> Create(IConnection connection);
    }
}