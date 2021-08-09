using MarineLang.Models;
using System;
using System.Linq;

namespace MarineLang.Streams
{
    public struct IndexedChar
    {
        public Position position;
        public char c;
    }

    public class IndexedCharStream : Stream<IndexedChar>
    {
        public override RangePosition RangePosition
            => new RangePosition(LastCurrent.position);

        public static IndexedCharStream Create(string str)
        {
            var linesStr
                = str.Split(new[] { Environment.NewLine }, StringSplitOptions.None).Select(line => line + Environment.NewLine).ToArray();

            var indexedChars =
                Enumerable.Range(0, linesStr.Length)
                .SelectMany(line =>
                    Enumerable.Range(0, linesStr[line].Length)
                    .Select(column =>
                        new IndexedChar
                        {
                            position = new Position(line + 1, column + 1),
                            c = linesStr[line][column]
                        }
                    ).Concat(new[] { new IndexedChar { c = ' ' } })
                );

            return new IndexedCharStream(indexedChars.ToArray());
        }

        public override ParserCore.IInput<IndexedChar> Advance()
        {
            return new IndexedCharStream(items, Index + 1);
        }

        IndexedCharStream(IndexedChar[] indexedChars, int index = 0) : base(indexedChars, index)
        {
        }
    }
}
