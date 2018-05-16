using System.Collections.Generic;

namespace Bur.Common.Extensions
{
    public static class ObjectExtensions
    {
        private const int seedPrimeNumber = 17;
        private const int fieldPrimeNumber = 23;


        public static int GetHashCodeFromArray<T>(this object obj, T[] array, IEqualityComparer<T> elementComparer = null)
        {
            unchecked
            {
                elementComparer = elementComparer ?? EqualityComparer<T>.Default;
                int hash = seedPrimeNumber;
                foreach (T element in array)
                {
                    hash = (hash * fieldPrimeNumber) + elementComparer.GetHashCode(element);
                }
                return hash;
            }
        }

        public static int GetHashCodeFromFields<T>(this object obj, T field)
        {
            return field.GetHashCode();
        }

        public static int GetHashCodeFromFields<T1, T2>(this object obj, T1 field1, T2 field2)
        {
            unchecked
            {
                int hash = seedPrimeNumber;
                hash = (hash * fieldPrimeNumber) + field1.GetHashCode();
                hash = (hash * fieldPrimeNumber) + field2.GetHashCode();
                return hash;
            }
        }

        public static int GetHashCodeFromFields<T1, T2, T3>(this object obj, T1 field1, T2 field2, T3 field3)
        {
            unchecked
            {
                int hash = seedPrimeNumber;
                hash = (hash * fieldPrimeNumber) + field1.GetHashCode();
                hash = (hash * fieldPrimeNumber) + field2.GetHashCode();
                hash = (hash * fieldPrimeNumber) + field3.GetHashCode();
                return hash;
            }
        }

        public static int GetHashCodeFromFields<T1, T2, T3, T4>(this object obj, T1 field1, T2 field2, T3 field3, T4 field4)
        {
            unchecked
            {
                int hash = seedPrimeNumber;
                hash = (hash * fieldPrimeNumber) + field1.GetHashCode();
                hash = (hash * fieldPrimeNumber) + field2.GetHashCode();
                hash = (hash * fieldPrimeNumber) + field3.GetHashCode();
                hash = (hash * fieldPrimeNumber) + field4.GetHashCode();
                return hash;
            }
        }

        public static int GetHashCodeFromFields<T1, T2, T3, T4, T5>(this object obj, T1 field1, T2 field2, T3 field3, T4 field4, T5 field5)
        {
            unchecked
            {
                int hash = seedPrimeNumber;
                hash = (hash * fieldPrimeNumber) + field1.GetHashCode();
                hash = (hash * fieldPrimeNumber) + field2.GetHashCode();
                hash = (hash * fieldPrimeNumber) + field3.GetHashCode();
                hash = (hash * fieldPrimeNumber) + field4.GetHashCode();
                hash = (hash * fieldPrimeNumber) + field5.GetHashCode();
                return hash;
            }
        }

        public static int GetHashCodeFromFields<T1, T2, T3, T4, T5, T6>(this object obj, T1 field1, T2 field2, T3 field3, T4 field4, T5 field5, T6 field6)
        {
            unchecked
            {
                int hash = seedPrimeNumber;
                hash = (hash * fieldPrimeNumber) + field1.GetHashCode();
                hash = (hash * fieldPrimeNumber) + field2.GetHashCode();
                hash = (hash * fieldPrimeNumber) + field3.GetHashCode();
                hash = (hash * fieldPrimeNumber) + field4.GetHashCode();
                hash = (hash * fieldPrimeNumber) + field5.GetHashCode();
                hash = (hash * fieldPrimeNumber) + field6.GetHashCode();
                return hash;
            }
        }

        public static int GetHashCodeFromFields<T1, T2, T3, T4, T5, T6, T7>(this object obj, T1 field1, T2 field2, T3 field3, T4 field4, T5 field5, T6 field6, T7 field7)
        {
            unchecked
            {
                int hash = seedPrimeNumber;
                hash = (hash * fieldPrimeNumber) + field1.GetHashCode();
                hash = (hash * fieldPrimeNumber) + field2.GetHashCode();
                hash = (hash * fieldPrimeNumber) + field3.GetHashCode();
                hash = (hash * fieldPrimeNumber) + field4.GetHashCode();
                hash = (hash * fieldPrimeNumber) + field5.GetHashCode();
                hash = (hash * fieldPrimeNumber) + field6.GetHashCode();
                hash = (hash * fieldPrimeNumber) + field7.GetHashCode();
                return hash;
            }
        }
    }
}
