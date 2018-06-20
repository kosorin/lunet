using System;

namespace Lure.Collections
{
    public sealed class ObjectPoolRef<TItem> : IDisposable
        where TItem : class
    {
        private readonly ObjectPool<TItem> _pool;
        private readonly TItem _item;

        internal ObjectPoolRef(ObjectPool<TItem> pool, TItem item)
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
