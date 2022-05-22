namespace Lunet.Common;

public readonly struct SortItem
{
    internal SortItem(int index, int value)
    {
        Index = index;
        Value = value;
    }

    public int Index { get; }

    internal int Value { get; }
}
