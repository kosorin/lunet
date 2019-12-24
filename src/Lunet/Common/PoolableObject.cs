using System;

namespace Lunet.Common
{
    internal class PoolableObject<TItem> : IPoolableObject<TItem>, IDisposable
        where TItem : PoolableObject<TItem>
    {
        private ObjectPool<TItem>? _owner;


        ObjectPool<TItem>? IPoolableObject<TItem>.Owner
        {
            get => _owner;
            set => _owner = value;
        }


        public void Return()
        {
            if (_owner == null)
            {
                throw new InvalidOperationException("Item is not owned by any object pool.");
            }

            _owner.Return((TItem)this);
        }

        protected virtual void OnRent()
        {
        }

        protected virtual void OnReturn()
        {
        }

        void IPoolableObject<TItem>.OnRent()
        {
            OnRent();
        }

        void IPoolableObject<TItem>.OnReturn()
        {
            OnReturn();
        }


        private bool _disposed;

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _owner = null;
            }

            _disposed = true;
        }
    }
}
