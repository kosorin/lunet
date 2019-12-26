﻿using Lunet.Common;
using Lunet.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lunet
{
    internal class FragmentGroup : PoolableObject<FragmentGroup>
    {
        private readonly Dictionary<byte, Fragment> _fragments = new Dictionary<byte, Fragment>();

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
            foreach (var fragment in _fragments.OrderBy(x => x.Key).Select(x => x.Value))
            {
                fragment.WriteTo(writer);
            }
        }

        protected override void OnReturn()
        {
            Clear();
        }

        private void Clear()
        {
            foreach (var fragment in _fragments.Values)
            {
                fragment.Return();
            }
            _fragments.Clear();
        }


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
    }

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
}
