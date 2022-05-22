namespace Lunet.Common;

internal sealed class ArrayEqualityComparer<TElement> : IEqualityComparer<TElement[]>
{
    private readonly IEqualityComparer<TElement> _elementComparer;

    public ArrayEqualityComparer() : this(EqualityComparer<TElement>.Default)
    {
    }

    public ArrayEqualityComparer(IEqualityComparer<TElement> elementComparer)
    {
        _elementComparer = elementComparer ?? throw new ArgumentNullException(nameof(elementComparer));
    }


    public bool Equals(TElement[]? x, TElement[]? y)
    {
        if (ReferenceEquals(x, y))
        {
            return true;
        }
        if (x is null || y is null)
        {
            return false;
        }
        if (x.Length != y.Length)
        {
            return false;
        }
        for (var i = 0; i < x.Length; i++)
        {
            if (!_elementComparer.Equals(x[i], y[i]))
            {
                return false;
            }
        }
        return true;
    }

    public int GetHashCode(TElement[] array)
    {
        var hashCode = new HashCode();

        if (array != null)
        {
            for (var i = 0; i < array.Length; i++)
            {
                hashCode.Add(array[i], _elementComparer);
            }
        }

        return hashCode.ToHashCode();
    }
}
