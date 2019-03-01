using Lure.Net.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lure.Net
{
    public class DefaultChannelFactory : IChannelFactory
    {
        private readonly IDictionary<byte, Func<byte, IConnection, IChannel>> _activators;

        public DefaultChannelFactory()
        {
            _activators = new Dictionary<byte, Func<byte, IConnection, IChannel>>();
        }

        public byte Add<TChannel>() where TChannel : IChannel
        {
            var id = GetNextId();
            var activator = ObjectActivatorFactory.CreateParameterizedAs<byte, IConnection, TChannel, IChannel>();
            _activators.Add(id, activator);
            return id;
        }

        public byte Add(Func<byte, IConnection, IChannel> activator)
        {
            var id = GetNextId();
            _activators.Add(id, activator);
            return id;
        }

        public void Clear()
        {
            _activators.Clear();
        }

        public IEnumerable<IChannel> Create(IConnection connection)
        {
            return _activators.Select(x => x.Value(x.Key, connection));
        }

        private byte GetNextId()
        {
            return (byte)_activators.Count;
        }
    }
}
