using System;
using System.Collections.Generic;
using System.Linq;

namespace Lure.Net
{
    public class ChannelFactory : IChannelFactory
    {
        private readonly IDictionary<byte, Func<byte, Connection, IChannel>> _activators;

        public ChannelFactory()
        {
            _activators = new Dictionary<byte, Func<byte, Connection, IChannel>>();
        }

        public byte Add<TChannel>() where TChannel : IChannel
        {
            var id = GetNextId();
            var activator = ObjectActivatorFactory.CreateParameterizedAs<byte, Connection, TChannel, IChannel>();
            _activators.Add(id, activator);
            return id;
        }

        public byte Add(Func<byte, Connection, IChannel> activator)
        {
            var id = GetNextId();
            _activators.Add(id, activator);
            return id;
        }

        public void Clear()
        {
            _activators.Clear();
        }

        public IDictionary<byte, IChannel> Create(Connection connection)
        {
            return _activators.ToDictionary(x => x.Key, x => x.Value(x.Key, connection));
        }

        private byte GetNextId()
        {
            return (byte)_activators.Count;
        }
    }
}
