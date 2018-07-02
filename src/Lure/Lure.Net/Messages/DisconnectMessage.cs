using Lure.Net.Data;

namespace Lure.Net.Messages
{
    [NetMessage(SystemMessageType.Disconnect)]
    internal sealed class DisconnectMessage : NetMessage
    {
        protected override void DeserializeCore(INetDataReader reader)
        {
        }

        protected override void SerializeCore(INetDataWriter writer)
        {
        }
    }
}
