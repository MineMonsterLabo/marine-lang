using MarineLang.Models;
using MarineLang.ParserCore;
using System.Collections.Generic;

namespace MarineLang.Inputs
{
    public class TokenInput : Input<Token>
    {
        public override RangePosition RangePosition => LastCurrent.rangePosition;

        public static TokenInput Create(IReadOnlyList<Token> tokens)
        {
            return new TokenInput(tokens);
        }

        TokenInput(IReadOnlyList<Token> tokens, int index = 0) : base(tokens, index)
        {
        }

        public override IInput<Token> Advance()
        {
            return new TokenInput(items, Index + 1);
        }
    }
}
