using Bur.Common;
using System;

namespace Bur.Net.Server
{
    public class NetClient : INetClient
    {
        private readonly IChannel channel;

        public NetClient(long id, IChannel channel)
        {
            Id = id;

            this.channel = channel;
            this.channel.DataReceived += Channel_DataReceived;
            this.channel.Stopped += Channel_Stopped;
        }

        public long Id { get; }

        public ConnectionState State => channel.GetCurrentState();

        public IEndPoint RemoteEndPoint => channel.RemoteEndPoint;

        public event TypedEventHandler<INetClient, DataReceivedEventArgs> DataReceived;

        public event TypedEventHandler<INetClient> Disconnected;

        public void Disconnect()
        {
            channel.Stop();
        }

        public ConnectionState GetCurrentState() => channel.GetCurrentState();

        private void Channel_DataReceived(IChannel sender, DataReceivedEventArgs e)
        {
            OnDataReceived(e);
        }

        private void Channel_Stopped(object sender, EventArgs e)
        {
            OnStopped();
        }

        private void OnDataReceived(DataReceivedEventArgs e)
        {
            DataReceived?.Invoke(this, e);
        }

        private void OnStopped()
        {
            Disconnected?.Invoke(this, EventArgs.Empty);
        }
    }
}
