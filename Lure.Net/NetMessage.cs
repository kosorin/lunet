namespace Lure.Net
{
    public interface INetMessage
    {
    }

    public class TextMessage : INetMessage
    {
        public string Text { get; set; }
    }
}
