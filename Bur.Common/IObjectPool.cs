using System;

namespace Bur.Common
{
    public interface IObjectPool<TItem> : IDisposable
    {
        TItem Rent();

        void Return(TItem item);
    }
}
