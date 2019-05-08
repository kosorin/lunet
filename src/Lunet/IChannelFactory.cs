using System.Collections.Generic;

namespace Lunet
{
    public interface IChannelFactory
    {
        IEnumerable<IChannel> Create(Connection connection);
    }
}