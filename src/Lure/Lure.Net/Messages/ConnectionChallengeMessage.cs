using Lure.Net.Data;

namespace Lure.Net.Messages
{
    [NetMessage(SystemMessageType.ConnectionChallenge)]
    internal sealed class ConnectionChallengeMessage : SystemMessage
    {
        protected override void Deserialize(INetDataReader reader)
        {
        }

        protected override void Serialize(INetDataWriter writer)
        {
        }
    }
}
