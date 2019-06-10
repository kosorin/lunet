using System;
using System.Collections.Concurrent;

namespace Lunet.Common
{
    internal class ObjectPool<TItem>
        where TItem : class
    {
        private readonly bool _isItemDisposable = typeof(IDisposable).IsAssignableFrom(typeof(TItem));
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

        public TItem Rent()
        {
            if (!_objects.TryDequeue(out var item))
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

            if (_objects.Count < _capacity)
            {
                _objects.Enqueue(item);
            }
            else
            {
                if (_isItemDisposable)
                {
                    ((IDisposable)item).Dispose();
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                if (_isItemDisposable)
                {
                    foreach (var item in _objects)
                    {
                        ((IDisposable)item).Dispose();
                    }
                }
            }
            _disposed = true;
        }
    }
}
