namespace Lure
{
    public interface ILockable
    {
        bool IsLocked { get; }

        void Lock();
    }
}
