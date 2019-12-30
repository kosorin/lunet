using Lunet.Common;

namespace Lunet.Extensions
{
    public static class ChannelSettingsExtensions
    {
        public static void SetChannel<TChannel>(this ChannelSettings channelSettings, byte channelId) where TChannel : Channel
        {
            channelSettings.SetChannel(channelId, ObjectActivatorFactory.CreateParameterizedAs<byte, Connection, TChannel, Channel>());
        }
    }
}
