using MarineLang.Models;

namespace MarineLang.ParserCore
{
    public interface IInput<T>
    {
        int Index { get; }
        bool IsEnd { get; }

        T Current { get; }
        T LastCurrent { get; }

        RangePosition RangePosition { get; }

        IInput<T> Advance();
    }
}
