using Lure.Net.Data;

namespace Lure.Net.Messages
{
    [NetMessage(SystemMessageType.Disconnect)]
    internal sealed class DisconnectMessage : SystemMessage
    {
        protected override void Deserialize(INetDataReader reader)
        {
        }

        protected override void Serialize(INetDataWriter writer)
        {
        }
    }
}
