using Serilog;
using System;
using System.Collections.Concurrent;

namespace Lure.Collections
{
    public class ObjectPool<TItem> : IObjectPool<TItem>
        where TItem : class
    {
        private readonly bool isItemDisposable = typeof(IDisposable).IsAssignableFrom(typeof(TItem));
        private readonly int _capacity;
        private readonly ObjectActivator<TItem> _activator;
        private readonly ConcurrentQueue<TItem> _objects;
        private bool _disposed;

        public ObjectPool()
            : this(int.MaxValue)
        {
        }

        public ObjectPool(ObjectActivator<TItem> activator)
            : this(int.MaxValue, activator)
        {
        }

        private ObjectPool(int capacity)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), capacity, $"Argument {nameof(capacity)} must be greater than zero.");
            }

            _capacity = capacity;
            _activator = ObjectActivatorFactory.Create<TItem>();
            _objects = new ConcurrentQueue<TItem>();
        }

        private ObjectPool(int capacity, ObjectActivator<TItem> activator)
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

        public event EventHandler<TItem> ItemCreated;

        public event EventHandler<TItem> ItemRented;

        public event EventHandler<TItem> ItemReturned;

        public event EventHandler<TItem> ItemDisposed;

        public TItem Rent()
        {
            TItem item;
            if (!_objects.TryDequeue(out item))
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
                OnItemReturned(item);
                _objects.Enqueue(item);
            }
            else
            {
                OnItemDisposed(item);
                Log.Verbose("ObjectPool<{ItemType}> overflow.", typeof(TItem).Name);
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
            if (item is IPoolable poolable)
            {
                poolable.OnRent();
            }
            ItemRented?.Invoke(this, item);
        }

        protected virtual void OnItemReturned(TItem item)
        {
            if (item is IPoolable poolable)
            {
                poolable.OnReturn();
            }
            ItemReturned?.Invoke(this, item);
        }

        protected virtual void OnItemDisposed(TItem item)
        {
            if (isItemDisposable)
            {
                ((IDisposable)item).Dispose();
            }
            ItemDisposed?.Invoke(this, item);
        }
    }
}
