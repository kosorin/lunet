namespace Bur.Common
{
    public interface IConfiguration
    {
        bool IsLocked { get; }

        void Lock();
    }
}
