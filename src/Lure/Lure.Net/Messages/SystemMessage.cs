namespace Lure.Net.Messages
{
    internal abstract class SystemMessage : NetMessage
    {
        public SystemMessageType Type => (SystemMessageType)TypeId;
    }
}
