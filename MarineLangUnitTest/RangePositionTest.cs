using MarineLang.Models;
using Xunit;

namespace MarineLangUnitTest
{
    public class RangePositionTest
    {
        [Theory]
        [InlineData(1, 1, 1, 1, 1, 1, true)]
        [InlineData(1, 1, 1, 10, 1, 8, true)]
        [InlineData(1, 1, 1, 10, 1, 10, true)]
        [InlineData(1, 1, 5, 1, 1, 10, true)]
        [InlineData(1, 1, 5, 1, 2, 10, true)]
        [InlineData(1, 1, 5, 1, 5, 1, true)]
        [InlineData(1, 1, 5, 1, 5, 2, false)]
        [InlineData(1, 1, 5, 1, 0, 10, false)]
        [InlineData(1, 1, 1, 1, 1, 2, false)]
        public void ContainTest(
            int startLine, int startColumn,
            int endLine, int endColumn,
            int line, int column,
            bool isContain
        )
        {
            var rangePosition
                = new RangePosition(new Position(0, startLine, startColumn), new Position(0, endLine, endColumn));
            var position = new Position(0, line, column);
            Assert.Equal(isContain, rangePosition.Contain(position));
        }

        [Theory]
        [InlineData(1, 1, 1, 1, 1, 1, 1, 1, true)]
        [InlineData(1, 1, 1, 10, 1, 10, 1, 10, true)]
        [InlineData(1, 5, 1, 10, 1, 1, 1, 5, true)]
        [InlineData(1, 5, 1, 10, 1, 6, 1, 8, true)]
        [InlineData(1, 5, 1, 6, 1, 7, 1, 8, false)]
        [InlineData(1, 7, 1, 8, 1, 5, 1, 6, false)]
        [InlineData(1, 5, 2, 10, 1, 15, 1, 16, true)]
        [InlineData(1, 5, 2, 10, 2, 10, 2, 10, true)]
        [InlineData(1, 5, 2, 10, 2, 11, 3, 4, false)]
        public void IntersectionTest(
           int startLine, int startColumn,
           int endLine, int endColumn,
           int start2Line, int start2Column,
           int end2Line, int end2Column,
           bool isContain
       )
        {
            var rangePosition
                = new RangePosition(new Position(0, startLine, startColumn), new Position(0, endLine, endColumn));
            var rangePosition2
                = new RangePosition(new Position(0, start2Line, start2Column), new Position(0, end2Line, end2Column));
            Assert.Equal(isContain, rangePosition.Intersection(rangePosition2));
        }
    }
}
