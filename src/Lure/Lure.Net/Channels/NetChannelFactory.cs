using System;
using System.Collections.Generic;
using System.Linq;

namespace Lure.Net.Channels
{
    public class NetChannelFactory : INetChannelFactory
    {
        private readonly IDictionary<byte, Func<Connection, INetChannel>> _activators;

        public NetChannelFactory()
        {
            _activators = new Dictionary<byte, Func<Connection, INetChannel>>();
        }

        internal NetChannelFactory(bool isDefault) : this()
        {
            if (isDefault)
            {
                Add<ReliableOrderedChannel>();
            }
        }

        public byte Add<TChannel>() where TChannel : INetChannel
        {
            var id = GetNextId();
            var activator = ObjectActivatorFactory.CreateParameterizedAs<Connection, TChannel, INetChannel>();
            _activators.Add(id, activator);
            return id;
        }

        public byte Add(Func<Connection, INetChannel> activator)
        {
            var id = GetNextId();
            _activators.Add(id, activator);
            return id;
        }

        public void Clear()
        {
            _activators.Clear();
        }

        public IDictionary<byte, INetChannel> Create(Connection connection)
        {
            return _activators.ToDictionary(x => x.Key, x => x.Value(connection));
        }

        private byte GetNextId()
        {
            return (byte)_activators.Count;
        }
    }
}
