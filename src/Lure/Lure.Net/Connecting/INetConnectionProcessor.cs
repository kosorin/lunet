using Lure.Net.Data;
using System.Net;

namespace Lure.Net
{
    public interface INetConnectionProcessor
    {
        INetSerializable CreateRequestData(IPEndPoint remoteEndPoint);
    }
}