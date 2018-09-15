using System;
using System.Collections.Generic;
using System.Linq;

namespace Lure.Net.Channels
{
    public class NetChannelFactory : INetChannelFactory
    {
        private readonly IDictionary<byte, Func<NetConnection, INetChannel>> _activators;

        public NetChannelFactory()
        {
            _activators = new Dictionary<byte, Func<NetConnection, INetChannel>>();
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
            var activator = ObjectActivatorFactory.CreateParameterizedAs<NetConnection, TChannel, INetChannel>();
            _activators.Add(id, activator);
            return id;
        }

        public byte Add(Func<NetConnection, INetChannel> activator)
        {
            var id = GetNextId();
            _activators.Add(id, activator);
            return id;
        }

        public void Clear()
        {
            _activators.Clear();
        }

        public IDictionary<byte, INetChannel> Create(NetConnection connection)
        {
            return _activators.ToDictionary(x => x.Key, x => x.Value(connection));
        }

        private byte GetNextId()
        {
            return (byte)_activators.Count;
        }
    }
}
