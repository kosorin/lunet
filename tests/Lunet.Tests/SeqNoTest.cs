using Xunit;

namespace Lunet.Tests
{
    public class SeqNoTest
    {
        [Theory]
        [InlineData(0, 0)]
        [InlineData(100, 100)]
        public void CompareTo_Equal(int l, int r)
        {
            var left = (SeqNo)l;
            var right = (SeqNo)r;

            var result = left.CompareTo(right);

            Assert.Equal(0, result);
        }

        [Theory]
        [InlineData(0, 100)]
        [InlineData(100, 0)]
        public void CompareTo_NotEqual(int l, int r)
        {
            var left = (SeqNo)l;
            var right = (SeqNo)r;

            var result = left.CompareTo(right);

            Assert.NotEqual(0, result);
        }

        [Theory]
        [InlineData(100, 100 + SeqNo.HalfRange - 1)]
        [InlineData(100, 100 + SeqNo.HalfRange - 2)]
        [InlineData(100, 100 + 1)]
        [InlineData(100, 100 - (SeqNo.HalfRange + 1))]
        [InlineData(100, 100 - (SeqNo.HalfRange + 2))]
        public void CompareTo_Less(int l, int r)
        {
            var left = (SeqNo)l;
            var right = (SeqNo)r;

            var result = left.CompareTo(right);

            Assert.True(result < 0);
        }

        [Theory]
        [InlineData(100, 100 + SeqNo.HalfRange + 2)]
        [InlineData(100, 100 + SeqNo.HalfRange + 1)]
        [InlineData(100, 100 + SeqNo.HalfRange + 0)]
        [InlineData(100, 100 - 1)]
        [InlineData(100, 100 - (SeqNo.HalfRange - 2))]
        [InlineData(100, 100 - (SeqNo.HalfRange - 1))]
        [InlineData(100, 100 - (SeqNo.HalfRange - 0))]
        public void CompareTo_Greater(int l, int r)
        {
            var left = (SeqNo)l;
            var right = (SeqNo)r;

            var result = left.CompareTo(right);

            Assert.True(result > 0);
        }

        [Theory]
        [InlineData(100, 100 + SeqNo.HalfRange - 1)]
        [InlineData(100, 100 + SeqNo.HalfRange - 2)]
        [InlineData(100, 100 + 1)]
        [InlineData(100, 100 - (SeqNo.HalfRange + 1))]
        [InlineData(100, 100 - (SeqNo.HalfRange + 2))]
        public void IsGreaterThan_False(int l, int r)
        {
            var left = (SeqNo)l;
            var right = (SeqNo)r;

            var result = left > right;

            Assert.False(result);
        }

        [Theory]
        [InlineData(100, 100 + SeqNo.HalfRange + 2)]
        [InlineData(100, 100 + SeqNo.HalfRange + 1)]
        [InlineData(100, 100 + SeqNo.HalfRange + 0)]
        [InlineData(100, 100 - 1)]
        [InlineData(100, 100 - (SeqNo.HalfRange - 2))]
        [InlineData(100, 100 - (SeqNo.HalfRange - 1))]
        [InlineData(100, 100 - (SeqNo.HalfRange - 0))]
        public void IsGreaterThan_True(int l, int r)
        {
            var left = (SeqNo)l;
            var right = (SeqNo)r;

            var result = left > right;

            Assert.True(result);
        }
    }
}
