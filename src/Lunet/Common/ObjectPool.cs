﻿using System;
using System.Collections.Concurrent;
using System.Threading;
using Lunet.Logging;

namespace Lunet.Common
{
    internal class ObjectPool<TItem> : IDisposable
        where TItem : class, IPoolableObject<TItem>
    {
        private static ILog Log { get; } = LogProvider.GetLogger(nameof(ObjectPool<TItem>));

        private readonly bool _isItemDisposable = typeof(IDisposable).IsAssignableFrom(typeof(TItem));
        private readonly Func<TItem> _activator;
        private readonly ConcurrentBag<TItem> _objects;

        private long _created = 0;
        private long _rented = 0;
        private long _returned = 0;

        public ObjectPool()
            : this(0)
        {
        }

        public ObjectPool(Func<TItem> activator)
            : this(activator, 0)
        {
            if (activator == null)
            {
                throw new ArgumentNullException(nameof(activator));
            }

            _activator = activator;
            _objects = new ConcurrentBag<TItem>();
        }

        public ObjectPool(int initialCapacity)
            : this(ObjectActivatorFactory.Create<TItem>(), initialCapacity)
        {
        }

        public ObjectPool(Func<TItem> activator, int initialCapacity)
        {
            if (activator == null)
            {
                throw new ArgumentNullException(nameof(activator));
            }

            _activator = activator;
            _objects = new ConcurrentBag<TItem>();

            for (int i = 0; i < initialCapacity; i++)
            {
                _objects.Add(CreateItem());
            }
        }

        public TItem Rent()
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }

            if (!_objects.TryTake(out var item))
            {
                item = CreateItem();
            }

            Interlocked.Increment(ref _rented);
            item.OnRent();

            return item;
        }

        public void Return(TItem item)
        {
            if (item == null)
            {
                return;
            }

            if (item.Owner != this)
            {
                throw new InvalidOperationException("Item is not owned by object pool.");
            }

            Interlocked.Increment(ref _returned);
            item.OnReturn();

            if (IsDisposed)
            {
                // Throwing ObjectDisposedException is not needed unlike Rent method
                if (_isItemDisposable)
                {
                    ((IDisposable)item).Dispose();
                }
                return;
            }

            _objects.Add(item);
        }


        private TItem CreateItem()
        {
            Interlocked.Increment(ref _created);

            var item = _activator.Invoke();
            item.Owner = this;

            return item;
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
                Log.Trace("Created={Created}; Rented={Rented}; Returned={Returned}", _created, _rented, _returned);

                if (_isItemDisposable)
                {
                    foreach (IDisposable item in _objects)
                    {
                        item.Dispose();
                    }
                }
                _objects.Clear();
            }
        }
    }
}
