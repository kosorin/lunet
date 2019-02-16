namespace Lure.Collections
{
    public interface IPoolable
    {
        void OnRent();

        void OnReturn();
    }
}
