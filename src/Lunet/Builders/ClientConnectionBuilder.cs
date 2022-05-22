using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lunet.Builders;

public class ClientConnectionBuilder
{
    private readonly ChannelFactoryBuilder _channelBuilder = new ChannelFactoryBuilder();

    private UdpEndPoint? _remoteEndPoint;
    private ILogger? _logger;

    public ClientConnection Build()
    {
        if (_remoteEndPoint == null)
        {
            throw new InvalidOperationException("Remote end point must be set.");
        }

        var channelFactory = _channelBuilder.Build();
        var logger = _logger ?? NullLogger.Instance;

        return new ClientConnection(_remoteEndPoint, channelFactory, logger);
    }

    public ClientConnectionBuilder ConnectTo(UdpEndPoint remoteEndPoint)
    {
        _remoteEndPoint = remoteEndPoint;

        return this;
    }

    public ClientConnectionBuilder ConfigureChannels(Action<ChannelFactoryBuilder> action)
    {
        action?.Invoke(_channelBuilder);

        return this;
    }

    public ClientConnectionBuilder UseLogger(ILogger logger)
    {
        _logger = logger;

        return this;
    }
}
