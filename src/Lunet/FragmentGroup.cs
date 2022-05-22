using Lunet.Common;
using Lunet.Data;

namespace Lunet;

internal class FragmentGroup : PoolableObject<FragmentGroup>
{
    private readonly Dictionary<byte, Fragment> _fragments = new Dictionary<byte, Fragment>();

    private Dictionary<SeqNo, FragmentGroup>? _container;


    private bool _disposed;

    protected override void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            Clear();
        }

        base.Dispose(disposing);

        _disposed = true;
    }

    public long Timestamp { get; set; }

    public SeqNo Seq { get; set; }

    public byte Count { get; set; }

    public bool IsComplete => _fragments.Count == Count;

    public bool CanAdd(byte index)
    {
        return index >= 0
            && index < Count
            && !_fragments.ContainsKey(index);
    }

    public void Add(Fragment fragment)
    {
        _fragments[fragment.Index] = fragment;
    }

    public void WriteTo(NetDataWriter writer)
    {
        if (!IsComplete)
        {
            throw new InvalidOperationException("Fragment group is not complete yet.");
        }

        foreach (var fragment in _fragments.OrderBy(x => x.Key).Select(x => x.Value))
        {
            fragment.WriteTo(writer);
        }
    }

    public void AttachTo(Dictionary<SeqNo, FragmentGroup> container)
    {
        _container = container;
        _container.Add(Seq, this);
    }


    protected override void OnReturn()
    {
        Clear();
    }


    private void Clear()
    {
        _container?.Remove(Seq);
        _container = null;

        foreach (var fragment in _fragments.Values)
        {
            fragment.Return();
        }
        _fragments.Clear();
    }
}
