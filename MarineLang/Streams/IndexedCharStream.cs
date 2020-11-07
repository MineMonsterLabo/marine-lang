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
        public static IndexedCharStream Create(string str)
        {
                = str.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            var linesStr

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

        IndexedCharStream(IndexedChar[] indexedChars) : base(indexedChars)
        {
        }
    }
}
