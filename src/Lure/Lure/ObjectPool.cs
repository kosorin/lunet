using System;
using System.Collections.Concurrent;

namespace Lure
{
    public class ObjectPool<TItem> : IObjectPool<TItem>
        where TItem : class
    {
        private readonly int _capacity;
        private readonly Func<TItem> _factory;
        private readonly ConcurrentQueue<TItem> _objects;
        private bool _disposed;

        public ObjectPool(Func<TItem> factory)
            : this(int.MaxValue, factory)
        {
        }

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

        public event EventHandler<TItem> Returned;

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

        public ObjectPoolRef<TItem> RentRef()
        {
            return new ObjectPoolRef<TItem>(this, Rent());
        }

        public void Return(TItem item)
        {
            if (item == null)
            {
                return;
            }

            OnItemReturned(item);

            if (_objects.Count < _capacity)
            {
                _objects.Enqueue(item);
            }
            else if (item is IDisposable disposable)
            {
                disposable.Dispose();
            }
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

        protected virtual void OnItemReturned(TItem item)
        {
            Returned?.Invoke(this, item);
        }
    }
}
