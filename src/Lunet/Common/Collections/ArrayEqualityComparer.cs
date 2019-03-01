using Lunet.Common.Extensions;
using System.Collections.Generic;

namespace Lunet.Common.Collections
{
    public sealed class ArrayEqualityComparer<TElement> : IEqualityComparer<TElement[]>
    {
        private static readonly EqualityComparer<TElement> DefaultElementComparer = EqualityComparer<TElement>.Default;

        private readonly IEqualityComparer<TElement> _elementComparer;

        public ArrayEqualityComparer() : this(DefaultElementComparer)
        {
        }

        public ArrayEqualityComparer(IEqualityComparer<TElement> elementComparer)
        {
            this._elementComparer = elementComparer;
        }


        public bool Equals(TElement[] x, TElement[] y)
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

        public int GetHashCode(TElement[] obj)
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
