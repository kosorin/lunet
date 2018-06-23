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

        [Fact]
        public void CompareTo_Performance()
        {
            for (int n = 0; n < 3; n++)
            {
                int less = 0;
                int equal = 0;
                int greater = 0;
                var sw = Stopwatch.StartNew();
                var a = SeqNo.Zero;
                for (int i = 0; i < SeqNo.Range; i++)
                {
                    var b = SeqNo.Zero;
                    for (int j = 0; j < SeqNo.Range; j += 32)
                    {
                        //if (a.GetDifference(b) < 0)
                        //{
                        //    less++;
                        //}
                        //if (a.GetDifference(b) == 0)
                        //{
                        //    equal++;
                        //}
                        //if (a.GetDifference(b) > 0)
                        //{
                        //    greater++;
                        //}

                        if (a < b)
                        {
                            less++;
                        }
                        if (a == b)
                        {
                            equal++;
                        }
                        if (a > b)
                        {
                            greater++;
                        }

                        b++;
                    }
                    a++;
                }
                sw.Stop();
                _output.WriteLine($"less:    {less} in {sw.ElapsedMilliseconds} ms");
                _output.WriteLine($"equal:   {equal} in {sw.ElapsedMilliseconds} ms");
                _output.WriteLine($"greater: {greater} in {sw.ElapsedMilliseconds} ms");
                _output.WriteLine("");
            }
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
