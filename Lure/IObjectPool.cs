using System;

namespace Lure
{
    public interface IObjectPool<TItem> : IDisposable
    {
        TItem Rent();

        void Return(TItem item);
    }

    public interface IObjectPool<TItem, TArg> : IDisposable
    {
        TItem Rent(TArg arg);

        void Return(TItem item);
    }
}
