namespace Lunet
{
    public interface IChannelFactory
    {
        Channel Create(byte channelId, Connection connection);

        bool TryCreate(byte channelId, Connection connection, out Channel channel);
    }
}