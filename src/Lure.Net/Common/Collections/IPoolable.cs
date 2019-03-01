namespace Lure.Net.Common.Collections
{
    public interface IPoolable
    {
        void OnRent();

        void OnReturn();
    }
}
