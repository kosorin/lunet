using Lure.Net.Data;

namespace Lure.Net.Messages
{
    [NetMessage(SystemMessageType.ConnectionRequest)]
    internal sealed class ConnectionRequestMessage : NetMessage
    {
        protected override void DeserializeCore(INetDataReader reader)
        {
        }

        protected override void SerializeCore(INetDataWriter writer)
        {
        }
    }
}
