using System;

namespace Lure
{
    public abstract class Equatable<T> : IEquatable<T> where T : Equatable<T>
    {
        public static bool operator ==(Equatable<T> a, Equatable<T> b)
        {
            if (a is null && b is null)
            {
                return true;
            }
            else if (a is null || b is null)
            {
                return false;
            }
            else
            {
                return a.Equals(b);
            }
        }

        public static bool operator !=(Equatable<T> a, Equatable<T> b)
        {
            return !(a == b);
        }

        public bool Equals(T other)
        {
            if (other == null)
            {
                return false;
            }
            else
            {
                return EqualsCore(other);
            }
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as T);
        }

        public override int GetHashCode()
        {
            return GetHashCodeCore();
        }

        protected abstract bool EqualsCore(T other);

        protected abstract int GetHashCodeCore();
    }
}
