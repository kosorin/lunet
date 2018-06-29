namespace Lure.Net
{
    public enum NetPeerState
    {
        Error,

        Unstarted,
        Starting,

        Running,

        Stopping,
        Stopped,
    }
}
