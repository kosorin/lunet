using Lure.Net.Data;

namespace Lure.Net.Messages
{
    [NetMessage(SystemMessageType.ConnectionChallenge)]
    internal sealed class ConnectionChallengeMessage : NetMessage
    {
        protected override void DeserializeCore(INetDataReader reader)
        {
        }

        protected override void SerializeCore(INetDataWriter writer)
        {
        }
    }
}
