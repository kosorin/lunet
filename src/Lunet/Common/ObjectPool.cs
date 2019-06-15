using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Lunet.Common
{
    internal class ObjectPool<TItem> : IDisposable
        where TItem : class
    {
        private readonly bool _isItemDisposable = typeof(IDisposable).IsAssignableFrom(typeof(TItem));
        private readonly Func<TItem> _activator;
        private readonly ConcurrentBag<TItem> _objects;

        public ObjectPool()
            : this(ObjectActivatorFactory.Create<TItem>())
        {
        }

        public ObjectPool(Func<TItem> activator)
        {
            if (activator == null)
            {
                throw new ArgumentNullException(nameof(activator));
            }

            _activator = activator;
            _objects = new ConcurrentBag<TItem>();
        }

        public TItem Rent()
        {
            if (IsDisposed)
            {
                return null!;
            }

            if (!_objects.TryTake(out var item))
            {
                item = _activator();
            }

            return item;
        }

        public void Return(TItem item)
        {
            if (item == null)
            {
                return;
            }

            if (IsDisposed && _isItemDisposable)
            {
                ((IDisposable)item).Dispose();
                return;
            }

            _objects.Add(item);
        }


        private int _disposed;

        public virtual bool IsDisposed => _disposed == 1;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (Interlocked.CompareExchange(ref _disposed, 1, 0) == 1)
            {
                return;
            }

            if (disposing)
            {
                if (_isItemDisposable)
                {
                    foreach (IDisposable item in _objects)
                    {
                        item.Dispose();
                    }
                }
            }
        }
    }
}
