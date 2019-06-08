using System;

namespace Lunet.Common.Collections
{
    public sealed class ObjectPoolRef<TItem> : IDisposable
        where TItem : class
    {
        private readonly IObjectPool<TItem> _pool;
        private readonly TItem _item;

        private bool _disposed;

        internal ObjectPoolRef(IObjectPool<TItem> pool, TItem item)
        {
            _pool = pool ?? throw new ArgumentNullException(nameof(pool));
            _item = item ?? throw new ArgumentNullException(nameof(item));
        }

        public TItem Instance => _item;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _pool.Return(_item);

            _disposed = true;
        }
    }
}
