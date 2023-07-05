using MarineLang.Models;
using MarineLang.ParserCore;
using System;
using System.Collections.Generic;

namespace MarineLang.Inputs
{
    public abstract class Input<T> : IInput<T>
    {
        protected readonly IReadOnlyList<T> items;

        public int Index { get; private set; }
        public bool IsEnd => items.Count <= Index;

        public T Current => items[Index];
        public T LastCurrent => items[Math.Min(Index, items.Count - 1)];

        public abstract RangePosition RangePosition { get; }

        public Input(IReadOnlyList<T> items, int index)
        {
            this.items = items;
            Index = index;
        }

        public abstract IInput<T> Advance();
    }
}
