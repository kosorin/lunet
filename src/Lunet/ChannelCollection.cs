using System;
using System.Collections;
using System.Collections.Generic;

namespace Lunet
{
    internal class ChannelCollection : IEnumerable<Channel>
    {
        private readonly ChannelSettings _settings;
        private readonly Dictionary<byte, Channel> _channels = new Dictionary<byte, Channel>();

        public ChannelCollection(ChannelSettings settings)
        {
            _settings = settings;
        }

        public Channel Get(byte channelId, Connection connection)
        {
            if (!TryGet(channelId, connection, out var channel))
            {
                throw new Exception($"Unknown channel '{channelId}'.");
            }

            return channel;
        }

        public bool TryGet(byte channelId, Connection connection, out Channel channel)
        {
            if (_channels.TryGetValue(channelId, out channel))
            {
                return true;
            }

            if (_settings.TryCreate(channelId, connection, out channel))
            {
                _channels[channelId] = channel;
                return true;
            }

            channel = null!;
            return false;
        }

        public IEnumerator<Channel> GetEnumerator()
        {
            return _channels.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
