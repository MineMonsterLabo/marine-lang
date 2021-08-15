using MarineLang.Models;
using System;
using System.Linq;

namespace MarineLang.Inputs
{
    public class CharInput : Input<char>
    {
        public override RangePosition RangePosition
            => new RangePosition(new Position(line, column));

        private readonly int line;
        private readonly int column;

        public static CharInput Create(string str)
        {
            return new CharInput(str.ToArray());
        }

        public override ParserCore.IInput<char> Advance()
        {
            if (items.Length <= Index + 1 || Environment.NewLine.Length > Index + 1)
            {
                return new CharInput(items, Index + 1, line, column + 1);
            }

            for (var i = 0; i < Environment.NewLine.Length; i++)
            {
                if (Environment.NewLine[Environment.NewLine.Length - 1 - i] != items[Index - i])
                    return new CharInput(items, Index + 1, line, column + 1);
            }
            return new CharInput(items, Index + 1, line + 1, 1);
        }

        CharInput(char[] chars, int index = 0, int line = 1, int column = 1) : base(chars, index)
        {
            this.line = line;
            this.column = column;
        }
    }
}
