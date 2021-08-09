﻿using MarineLang.Models;
using System;
using System.Linq;

namespace MarineLang.Inputs
{
    public struct IndexedChar
    {
        public Position position;
        public char c;
    }

    public class IndexedCharInput : Input<IndexedChar>
    {
        public override RangePosition RangePosition
            => new RangePosition(LastCurrent.position);

        public static IndexedCharInput Create(string str)
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

            return new IndexedCharInput(indexedChars.ToArray());
        }

        public override ParserCore.IInput<IndexedChar> Advance()
        {
            return new IndexedCharInput(items, Index + 1);
        }

        IndexedCharInput(IndexedChar[] indexedChars, int index = 0) : base(indexedChars, index)
        {
        }
    }
}