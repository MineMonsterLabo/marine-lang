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
            var linsStr
                = str.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

            var indexedChars =
                Enumerable.Range(0, linsStr.Length)
                .SelectMany(line =>
                    Enumerable.Range(0, linsStr[line].Length)
                    .Select(column =>
                        new IndexedChar
                        {
                            position = new Position(line + 1, column + 1),
                            c = linsStr[line][column]
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
