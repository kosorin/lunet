using System;
using System.Collections.Generic;
using System.Linq;

namespace Lure.Net.Channels
{
    public class NetChannelFactory : ILockable
    {
        private readonly IDictionary<byte, Func<NetConnection, INetChannel>> _activators;

        public NetChannelFactory()
        {
            _activators = new Dictionary<byte, Func<NetConnection, INetChannel>>();
        }

        public bool IsLocked { get; private set; }

        public void Lock()
        {
            IsLocked = true;
        }

        public byte Register<TChannel>() where TChannel : INetChannel
        {
            ThrowIfLocked();
            var id = GetNextId();
            var activator = ObjectActivatorFactory.CreateParameterizedAs<NetConnection, TChannel, INetChannel>();
            _activators.Add(id, activator);
            return id;
        }

        public byte Register(Func<NetConnection, INetChannel> activator)
        {
            ThrowIfLocked();
            var id = GetNextId();
            _activators.Add(id, activator);
            return id;
        }

        public void Clear()
        {
            ThrowIfLocked();
            _activators.Clear();
        }

        public IDictionary<byte, INetChannel> Create(NetConnection connection)
        {
            if (_activators.Count == 0)
            {
                return new Dictionary<byte, INetChannel>
                {
                    [NetConnection.DefaultChannelId] = new ReliableOrderedChannel(connection)
                };
            }
            return _activators.ToDictionary(x => x.Key, x => x.Value(connection));
        }

        private byte GetNextId()
        {
            return (byte)(NetConnection.DefaultChannelId + _activators.Count);
        }

        private void ThrowIfLocked()
        {
            if (IsLocked)
            {
                throw new NetException("Object was locked.");
            }
        }
    }
}
