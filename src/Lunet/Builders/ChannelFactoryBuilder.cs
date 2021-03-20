using Lunet.Common;
using System;
using System.Collections.Generic;

namespace Lunet.Builders
{
    public class ChannelFactoryBuilder
    {
        private readonly Dictionary<byte, ChannelConstructor> _activators = new Dictionary<byte, ChannelConstructor>();

        internal ChannelFactoryBuilder()
        {
        }

        internal ChannelFactory Build()
        {
            if (_activators.Count < 1)
            {
                throw new InvalidOperationException("Must be set at least one channel.");
            }

            return new ChannelFactory(_activators);
        }

        public ChannelFactoryBuilder AddChannel(byte channelId, ChannelConstructor activator)
        {
            _activators[channelId] = activator;

            return this;
        }

        public ChannelFactoryBuilder AddChannel<TChannel>(byte channelId) where TChannel : Channel
        {
            return AddChannel(channelId, new ChannelConstructor(ObjectActivatorFactory.CreateParameterizedAs<byte, Connection, TChannel, Channel>()));
        }
    }
}
