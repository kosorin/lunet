using Lunet.Channels;
using Lunet.Extensions;
using System;
using System.Collections.Generic;

namespace Lunet
{
    public class ChannelSettings
    {
        public static byte DefaultChannelId { get; } = 0;

        public static ChannelSettings Default { get; }

        static ChannelSettings()
        {
            var settings = new ChannelSettings();
            settings.SetChannel<ReliableOrderedChannel>(DefaultChannelId);
            Default = settings;
        }


        private readonly Dictionary<byte, Func<byte, Connection, Channel>> _activators = new Dictionary<byte, Func<byte, Connection, Channel>>();

        public bool IsLocked { get; private set; }

        public Channel Create(byte channelId, Connection connection)
        {
            if (!TryCreate(channelId, connection, out var channel))
            {
                throw new Exception($"Unknown channel settings '{channelId}'.");
            }

            return channel;
        }

        public bool TryCreate(byte channelId, Connection connection, out Channel channel)
        {
            IsLocked = true;

            if (!_activators.TryGetValue(channelId, out var activator))
            {
                channel = null!;
                return false;
            }

            channel = activator.Invoke(channelId, connection);
            return true;
        }

        public void SetChannel(byte channelId, Func<byte, Connection, Channel> activator)
        {
            if (IsLocked)
            {
                throw new InvalidOperationException("Channel settings is locked.");
            }

            _activators[channelId] = activator;
        }
    }
}
