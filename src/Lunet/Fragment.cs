using Lunet.Common;
using Lunet.Data;

namespace Lunet;

internal class Fragment : PoolableObject<Fragment>
{
    private readonly byte[] _data = new byte[ushort.MaxValue];
    private int _length;

    public byte Index { get; private set; }

    public void Set(byte index, ReadOnlySpan<byte> data)
    {
        Index = index;

        _length = data.Length;
        data.CopyTo(new Span<byte>(_data, 0, _length));
    }

    public void WriteTo(NetDataWriter writer)
    {
        writer.WriteSpan(new ReadOnlySpan<byte>(_data, 0, _length));
    }
}
