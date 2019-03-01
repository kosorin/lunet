 using System;

namespace Lunet.Common.Collections
{
    public interface IObjectPool<TItem> : IDisposable
    {
        TItem Rent();

        void Return(TItem item);
    }
}
