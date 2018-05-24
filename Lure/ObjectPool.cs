using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Lure
{
    public class ObjectPool<TItem> : IObjectPool<TItem>
    {
        private readonly int _capacity;
        private readonly Func<TItem> _factory;
        private readonly ConcurrentQueue<TItem> _objects;
        private bool _disposed;

        public ObjectPool(int capacity, Func<TItem> factory)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), capacity, $"Argument {nameof(capacity)} must be greater than zero.");
            }
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            _capacity = capacity;
            _factory = factory;
            _objects = new ConcurrentQueue<TItem>();
        }


        public TItem Rent()
        {
            if (_objects.TryDequeue(out var item))
            {
                return item;
            }
            else
            {
                return _factory();
            }
        }

        public void Return(TItem item)
        {
            if (_objects.Count > _capacity)
            {
                if (item is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            _objects.Enqueue(item);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (typeof(IDisposable).IsAssignableFrom(typeof(TItem)))
                    {
                        foreach (IDisposable disposable in _objects)
                        {
                            disposable.Dispose();
                        }
                    }
                }
                _disposed = true;
            }
        }
    }
}
