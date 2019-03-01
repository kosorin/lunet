namespace Lunet.Common.Collections
{
    public interface IPoolable
    {
        void OnRent();

        void OnReturn();
    }
}
