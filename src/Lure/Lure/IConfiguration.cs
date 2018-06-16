namespace Lure
{
    public interface IConfiguration
    {
        bool IsLocked { get; }

        void Lock();
    }
}
