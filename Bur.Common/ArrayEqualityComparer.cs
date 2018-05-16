using Bur.Common.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

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
            if (ReferenceEquals(x, null))
            {
                return false;
            }
            if (ReferenceEquals(y, null))
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
