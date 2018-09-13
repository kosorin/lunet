 using System;

namespace Lure.Collections
{
    public interface IObjectPool<TItem> : IDisposable
    {
        TItem Rent();

        void Return(TItem item);
    }
}
