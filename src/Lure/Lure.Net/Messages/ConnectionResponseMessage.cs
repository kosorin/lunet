using Lure.Net.Data;

namespace Lure.Net.Messages
{
    [NetMessage(SystemMessageType.ConnectionResponse)]
    internal sealed class ConnectionResponseMessage : SystemMessage
    {
        protected override void Deserialize(INetDataReader reader)
        {
        }

        protected override void Serialize(INetDataWriter writer)
        {
        }
    }
}
