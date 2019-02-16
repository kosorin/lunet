using System.Collections.Generic;

namespace Lure.Net
{
    public interface IChannelFactory
    {
        IDictionary<byte, IChannel> Create(Connection connection);
    }
}