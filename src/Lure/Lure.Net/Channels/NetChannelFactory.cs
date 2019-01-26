using System;
using System.Collections.Generic;
using System.Linq;

namespace Lure.Net.Channels
{
    public class NetChannelFactory : INetChannelFactory
    {
        private readonly IDictionary<byte, Func<byte, Connection, INetChannel>> _activators;

        public NetChannelFactory()
        {
            _activators = new Dictionary<byte, Func<byte, Connection, INetChannel>>();
        }

        public byte Add<TChannel>() where TChannel : INetChannel
        {
            var id = GetNextId();
            var activator = ObjectActivatorFactory.CreateParameterizedAs<byte, Connection, TChannel, INetChannel>();
            _activators.Add(id, activator);
            return id;
        }

        public byte Add(Func<byte, Connection, INetChannel> activator)
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
            return _activators.ToDictionary(x => x.Key, x => x.Value(x.Key, connection));
        }

        private byte GetNextId()
        {
            return (byte)_activators.Count;
        }
    }
}
