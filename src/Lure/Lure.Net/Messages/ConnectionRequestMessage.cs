using Lure.Net.Data;

namespace Lure.Net.Messages
{
    [NetMessage(SystemMessageType.ConnectionRequest)]
    internal sealed class ConnectionRequestMessage : SystemMessage
    {
        protected override void Deserialize(INetDataReader reader)
        {
        }

        protected override void Serialize(INetDataWriter writer)
        {
        }
    }
}
