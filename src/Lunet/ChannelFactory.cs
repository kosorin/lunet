using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Lunet
{
    internal class ChannelFactory
    {
        private readonly Dictionary<byte, ChannelConstructor> _activators;

        public ChannelFactory(Dictionary<byte, ChannelConstructor> activators)
        {
            _activators = activators;
        }

        public Channel Create(byte channelId, Connection connection)
        {
            if (!TryCreate(channelId, connection, out var channel))
            {
                throw new Exception($"Unknown channel '{channelId}'.");
            }

            return channel;
        }

        public bool TryCreate(byte channelId, Connection connection, [MaybeNullWhen(false)] out Channel channel)
        {
            if (!_activators.TryGetValue(channelId, out var activator))
            {
                channel = null;
                return false;
            }

            channel = activator.Invoke(channelId, connection);
            return true;
        }
    }
}

