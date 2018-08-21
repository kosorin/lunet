using Lure.Net.Data;

namespace Lure.Net.Messages
{
    [NetMessage(SystemMessageType.ConnectionReject)]
    internal sealed class ConnectionRejectMessage : SystemMessage
    {
        protected override void Deserialize(INetDataReader reader)
        {
        }

        protected override void Serialize(INetDataWriter writer)
        {
        }
    }
}
