using Lure.Extensions;
using System.Collections.Generic;

namespace Lure.Collections
{
    public sealed class ArrayEqualityComparer<T> : IEqualityComparer<T[]>
    {
        private static readonly EqualityComparer<T> DefaultElementComparer = EqualityComparer<T>.Default;

        private readonly IEqualityComparer<T> _elementComparer;

        public ArrayEqualityComparer() : this(DefaultElementComparer)
        {
        }

        public ArrayEqualityComparer(IEqualityComparer<T> elementComparer)
        {
            this._elementComparer = elementComparer;
        }


        public bool Equals(T[] x, T[] y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }
            if (x is null)
            {
                return false;
            }
            if (y is null)
            {
                return false;
            }
            if (x.Length != y.Length)
            {
                return false;
            }
            for (int i = 0; i < x.Length; i++)
            {
                if (!_elementComparer.Equals(x[i], y[i]))
                {
                    return false;
                }
            }
            return true;
        }

        public int GetHashCode(T[] obj)
        {
            unchecked
            {
                if (obj == null)
                {
                    return default;
                }
                return this.GetHashCodeFromArray(obj, _elementComparer);
            }
        }
    }
}
