using System;
using System.Collections.Concurrent;

namespace Lure.Collections
{
    public class ObjectPool<TItem> : IObjectPool<TItem>
        where TItem : class
    {
        private readonly int _capacity;
        private readonly Func<TItem> _factory;
        private readonly ConcurrentQueue<TItem> _objects;
        private bool _disposed;

        public ObjectPool()
            : this(int.MaxValue)
        {
        }

        public ObjectPool(int capacity)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), capacity, $"Argument {nameof(capacity)} must be greater than zero.");
            }

            var activator = ObjectActivatorFactory.Create<TItem>();

            _capacity = capacity;
            _factory = () => activator();
            _objects = new ConcurrentQueue<TItem>();
        }

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

        public event EventHandler<TItem> Rented;

        public event EventHandler<TItem> Returned;

        public TItem Rent()
        {
            TItem item;
            if (!_objects.TryDequeue(out item))
            {
                item = _factory();
            }

            OnItemRented(item);

            return item;
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

        protected virtual void OnItemRented(TItem item)
        {
            if (item is IPoolable poolable)
            {
                poolable.OnRent();
            }
            Rented?.Invoke(this, item);
        }

        protected virtual void OnItemReturned(TItem item)
        {
            if (item is IPoolable poolable)
            {
                poolable.OnReturn();
            }
            Returned?.Invoke(this, item);
        }
    }
}
