using System;

namespace Bur.Net
{
    public class ChannelConnectedEventArgs : EventArgs
    {
        public ChannelConnectedEventArgs(IChannel channel)
        {
            Channel = channel;
        }

        public IChannel Channel { get; }
    }
}
