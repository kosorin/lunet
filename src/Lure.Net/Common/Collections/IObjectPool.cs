 using System;

namespace Lure.Net.Common.Collections
{
    public interface IObjectPool<TItem> : IDisposable
    {
        TItem Rent();

        void Return(TItem item);
    }
}
