using System;
using System.Collections.Concurrent;

namespace Lunet.Common.Collections
{
    public class ObjectPool<TItem> : IObjectPool<TItem>
        where TItem : class
    {
        private readonly bool _isItemDisposable = typeof(IDisposable).IsAssignableFrom(typeof(TItem));
        private readonly bool _isItemPoolable = typeof(IPoolable).IsAssignableFrom(typeof(TItem));
        private readonly int _capacity;
        private readonly Func<TItem> _activator;
        private readonly ConcurrentQueue<TItem> _objects;

        private bool _disposed;

        public ObjectPool()
            : this(int.MaxValue)
        {
        }

        public ObjectPool(Func<TItem> activator)
            : this(int.MaxValue, activator)
        {
        }

        private ObjectPool(int capacity)
            : this(capacity, ObjectActivatorFactory.Create<TItem>())
        {
        }

        private ObjectPool(int capacity, Func<TItem> activator)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), capacity, $"Argument {nameof(capacity)} must be greater than zero.");
            }
            if (activator == null)
            {
                throw new ArgumentNullException(nameof(activator));
            }

            _capacity = capacity;
            _activator = activator;
            _objects = new ConcurrentQueue<TItem>();
        }

        public event TypedEventHandler<IObjectPool<TItem>, TItem> ItemCreated;

        public event TypedEventHandler<IObjectPool<TItem>, TItem> ItemRented;

        public event TypedEventHandler<IObjectPool<TItem>, TItem> ItemReturned;

        public event TypedEventHandler<IObjectPool<TItem>, TItem> ItemDisposed;

        public TItem Rent()
        {
            if (!_objects.TryDequeue(out var item))
            {
                item = _activator();
                OnItemCreated(item);
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

            if (_objects.Count < _capacity)
            {
                _objects.Enqueue(item);
                OnItemReturned(item);
            }
            else
            {
                OnItemDisposed(item);
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
                    foreach (var item in _objects)
                    {
                        OnItemDisposed(item);
                    }
                }
                _disposed = true;
            }
        }

        protected virtual void OnItemCreated(TItem item)
        {
            ItemCreated?.Invoke(this, item);
        }

        protected virtual void OnItemRented(TItem item)
        {
            if (_isItemPoolable)
            {
                ((IPoolable)item).OnRent();
            }
            ItemRented?.Invoke(this, item);
        }

        protected virtual void OnItemReturned(TItem item)
        {
            if (_isItemPoolable)
            {
                ((IPoolable)item).OnReturn();
            }
            ItemReturned?.Invoke(this, item);
        }

        protected virtual void OnItemDisposed(TItem item)
        {
            if (_isItemDisposable)
            {
                ((IDisposable)item).Dispose();
            }
            ItemDisposed?.Invoke(this, item);
        }
    }
}
