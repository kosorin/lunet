using System.Collections.Generic;

namespace Lure.Net.Channels
{
    public interface INetChannelFactory
    {
        IDictionary<byte, INetChannel> Create(Connection connection);
    }
}