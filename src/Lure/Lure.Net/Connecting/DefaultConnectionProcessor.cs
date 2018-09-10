using Lure.Net.Data;
using System.Net;

namespace Lure.Net
{
    public class DefaultConnectionProcessor : INetConnectionProcessor
    {
        public INetSerializable CreateRequestData(IPEndPoint remoteEndPoint)
        {
            return null;
        }
    }
}
