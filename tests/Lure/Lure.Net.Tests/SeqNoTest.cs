using System;
using System.Diagnostics;
using Xunit;
using Xunit.Abstractions;

namespace Lure.Net.Tests
{
    public class SeqNoTest
    {
        private readonly ITestOutputHelper _output;

        public SeqNoTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(100, 100)]
        public void CompareTo_Equal(int l, int r)
        {
            var left = (SeqNo)l;
            var right = (SeqNo)r;
            Assert.Equal(0, left.CompareTo(right));
        }

        [Theory]
        [InlineData(0, 100)]
        [InlineData(100, 0)]
        public void CompareTo_NotEqual(int l, int r)
        {
            var left = (SeqNo)l;
            var right = (SeqNo)r;
            Assert.NotEqual(0, left.CompareTo(right));
        }

        [Theory]
        [InlineData(42, 42 + SeqNo.HalfRange - 1)]
        [InlineData(42, 42 + SeqNo.HalfRange - 2)]
        [InlineData(42, 42 + 1)]
        [InlineData(42, 42 - (SeqNo.HalfRange + 1))]
        [InlineData(42, 42 - (SeqNo.HalfRange + 2))]
        public void CompareTo_Less(int l, int r)
        {
            var left = (SeqNo)l;
            var right = (SeqNo)r;
            Assert.True(left.CompareTo(right) < 0);
        }

        [Theory]
        [InlineData(42, 42 + SeqNo.HalfRange + 2)]
        [InlineData(42, 42 + SeqNo.HalfRange + 1)]
        [InlineData(42, 42 + SeqNo.HalfRange + 0)]
        [InlineData(42, 42 - 1)]
        [InlineData(42, 42 - (SeqNo.HalfRange - 2))]
        [InlineData(42, 42 - (SeqNo.HalfRange - 1))]
        [InlineData(42, 42 - (SeqNo.HalfRange - 0))]
        public void CompareTo_Greater(int l, int r)
        {
            var left = (SeqNo)l;
            var right = (SeqNo)r;
            Assert.True(left.CompareTo(right) > 0);
        }

        [Theory]
        [InlineData(42, 42 + SeqNo.HalfRange - 1)]
        [InlineData(42, 42 + SeqNo.HalfRange - 2)]
        [InlineData(42, 42 + 1)]
        [InlineData(42, 42 - (SeqNo.HalfRange + 1))]
        [InlineData(42, 42 - (SeqNo.HalfRange + 2))]
        public void IsGreaterThan_False(int l, int r)
        {
            var left = (SeqNo)l;
            var right = (SeqNo)r;
            Assert.False(left > right);
        }

        [Theory]
        [InlineData(42, 42 + SeqNo.HalfRange + 2)]
        [InlineData(42, 42 + SeqNo.HalfRange + 1)]
        [InlineData(42, 42 + SeqNo.HalfRange + 0)]
        [InlineData(42, 42 - 1)]
        [InlineData(42, 42 - (SeqNo.HalfRange - 2))]
        [InlineData(42, 42 - (SeqNo.HalfRange - 1))]
        [InlineData(42, 42 - (SeqNo.HalfRange - 0))]
        public void IsGreaterThan_True(int l, int r)
        {
            var left = (SeqNo)l;
            var right = (SeqNo)r;
            Assert.True(left > right);
        }
    }
}
