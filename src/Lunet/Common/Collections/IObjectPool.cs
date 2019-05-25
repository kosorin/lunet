using System;

namespace Lunet.Common.Collections
{
    public interface IObjectPool<TItem> : IDisposable
    {
        event TypedEventHandler<IObjectPool<TItem>, TItem> ItemCreated;

        event TypedEventHandler<IObjectPool<TItem>, TItem> ItemRented;

        event TypedEventHandler<IObjectPool<TItem>, TItem> ItemReturned;

        event TypedEventHandler<IObjectPool<TItem>, TItem> ItemDisposed;

        TItem Rent();

        void Return(TItem item);
    }
}
