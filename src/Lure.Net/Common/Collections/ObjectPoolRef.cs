using System;

namespace Lure.Net.Common.Collections
{
    public sealed class ObjectPoolRef<TItem> : IDisposable
        where TItem : class
    {
        private readonly IObjectPool<TItem> _pool;
        private readonly TItem _item;

        internal ObjectPoolRef(IObjectPool<TItem> pool, TItem item)
        {
            _pool = pool ?? throw new ArgumentNullException(nameof(pool));
            _item = item ?? throw new ArgumentNullException(nameof(item));
        }

        public TItem Instance => _item;

        public void Dispose()
        {
            _pool.Return(_item);
        }
    }
}
