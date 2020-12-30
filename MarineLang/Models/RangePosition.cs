using System;

namespace MarineLang.Models
{
   public class RangePosition:IEquatable<RangePosition>
    {
        public Position Start{ get; }
        public Position End{ get; }

        public RangePosition(Position start,Position end)
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
            return Start.Equals( other.Start )&& End.Equals(other.End);
        }
    }
}
