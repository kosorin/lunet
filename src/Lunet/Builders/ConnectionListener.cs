using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;

namespace Lunet.Builders
{
    public class ConnectionListenerBuilder
    {
        private readonly ChannelFactoryBuilder _channelBuilder = new ChannelFactoryBuilder();

        private UdpEndPoint? _localEndPoint;
        private ILogger? _logger;

        public ConnectionListenerBuilder()
        {
        }

        public ConnectionListener Build()
        {
            if (_localEndPoint == null)
            {
                throw new InvalidOperationException("Local end point must be set.");
            }

            var channelFactory = _channelBuilder.Build();
            var logger = _logger ?? NullLogger.Instance;

            return new ConnectionListener(_localEndPoint, channelFactory, logger);
        }

        public ConnectionListenerBuilder ListenOn(UdpEndPoint localEndPoint)
        {
            _localEndPoint = localEndPoint;

            return this;
        }

        public ConnectionListenerBuilder ConfigureChannels(Action<ChannelFactoryBuilder> action)
        {
            action?.Invoke(_channelBuilder);

            return this;
        }

        public ConnectionListenerBuilder UseLogger(ILogger logger)
        {
            _logger = logger;

            return this;
        }
    }
}
