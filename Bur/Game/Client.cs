using Bur.Net.Server;

namespace Bur.Game
{
    public class Client
    {
        private readonly INetClient client;

        public Client(INetClient client)
        {
            this.client = client;
        }
    }
}
