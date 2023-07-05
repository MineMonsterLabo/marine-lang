using MarineLang.Models;
using System;
using System.Collections;
using System.Collections.Generic;

namespace MarineLang.Inputs
{
    public class CharInput : Input<char>
    {
        public override RangePosition RangePosition
            => new RangePosition(new Position(Index, line, column));

        private readonly int line;
        private readonly int column;

        public class CharArray : IReadOnlyList<char>
        {
            private readonly string str;

            public CharArray(string str)
            {
                this.str = str;
            }

            public char this[int index] => str[index];

            public int Count => str.Length;

            public IEnumerator<char> GetEnumerator()
            {
                return str.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return str.GetEnumerator();
            }
        }

        public static CharInput Create(string str)
        {
            return new CharInput(new CharArray(str));
        }

        public override ParserCore.IInput<char> Advance()
        {
            if (items.Count <= Index + 1 || Environment.NewLine.Length > Index + 1)
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

        CharInput(IReadOnlyList<char> chars, int index = 0, int line = 1, int column = 1) : base(chars, index)
        {
            this.line = line;
            this.column = column;
        }
    }
}
