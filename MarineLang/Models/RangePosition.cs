using System;

namespace MarineLang.Models
{
    public class RangePosition : IEquatable<RangePosition>
    {
        public Position Start { get; }
        public Position End { get; }

        public RangePosition(Position start, Position end)
        {
            Start = start;
            End = end;
        }

        public override string ToString()
        {
            return $"{Start}～{End}";
        }

        public bool Equals(RangePosition other)
        {
            return Start.Equals(other.Start) && End.Equals(other.End);
        }

        public bool Contain(Position position)
        {
            return
                Start.line < position.line &&
                End.line > position.line
                ||
                Start.line == position.line &&
                End.line == position.line &&
                Start.column <= position.column &&
                End.column >= position.column
                ||
                Start.line == position.line &&
                Start.line != End.line &&
                Start.column <= position.column
                ||
                End.line == position.line &&
                Start.line != End.line &&
                End.column >= position.column;
        }

        public bool Intersection(RangePosition rangePosition)
        {
            return
                Contain(rangePosition.Start) ||
                Contain(rangePosition.End) ||
                rangePosition.Contain(Start) ||
                rangePosition.Contain(End);
        }
    }
}
