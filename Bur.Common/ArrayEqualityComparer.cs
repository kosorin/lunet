using Bur.Common.Extensions;
using System.Collections.Generic;

namespace Bur.Common
{
    public sealed class ArrayEqualityComparer<T> : IEqualityComparer<T[]>
    {
        private static readonly EqualityComparer<T> DefaultElementComparer = EqualityComparer<T>.Default;

        private readonly IEqualityComparer<T> elementComparer;

        public ArrayEqualityComparer() : this(DefaultElementComparer)
        {
        }

        public ArrayEqualityComparer(IEqualityComparer<T> elementComparer)
        {
            this.elementComparer = elementComparer;
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
                if (!elementComparer.Equals(x[i], y[i]))
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
                return this.GetHashCodeFromArray(obj, elementComparer);
            }
        }
    }
}
