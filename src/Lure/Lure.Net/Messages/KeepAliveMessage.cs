using Lure.Net.Data;

namespace Lure.Net.Messages
{
    [NetMessage(SystemMessageType.KeepAlive)]
    internal sealed class KeepAliveMessage : NetMessage
    {
        protected override void DeserializeCore(INetDataReader reader)
        {
        }

        protected override void SerializeCore(INetDataWriter writer)
        {
        }
    }
}
