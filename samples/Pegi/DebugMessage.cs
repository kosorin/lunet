using MessagePack;

namespace Pegi
{
    [MessagePackObject]
    public class DebugMessage
    {
        [Key(0)]
        public int Id { get; set; }

        [Key(1)]
        public string Text { get; set; }

        public override string ToString()
        {
            return $"ID {Id,6}";
        }
    }
}
