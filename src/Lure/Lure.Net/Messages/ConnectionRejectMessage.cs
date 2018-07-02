using Lure.Net.Data;

namespace Lure.Net.Messages
{
    [NetMessage(SystemMessageType.ConnectionReject)]
    internal sealed class ConnectionRejectMessage : NetMessage
    {
        protected override void DeserializeCore(INetDataReader reader)
        {
        }

        protected override void SerializeCore(INetDataWriter writer)
        {
        }
    }
}
