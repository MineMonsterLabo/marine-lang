using MarineLang.Models;
using MarineLang.ParserCore;
using System;

namespace MarineLang.Inputs
{
    public abstract class Input<T> : IInput<T>
    {
        protected readonly T[] items;

        public int Index { get; private set; }
        public bool IsEnd => items.Length <= Index;

        public T Current => items[Index];
        public T LastCurrent => items[Math.Min(Index, items.Length - 1)];

        public abstract RangePosition RangePosition { get; }

        public Input(T[] items, int index)
        {
            this.items = items;
            Index = index;
        }

        public abstract IInput<T> Advance();
    }
}
