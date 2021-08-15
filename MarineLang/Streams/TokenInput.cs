using MarineLang.Models;
using MarineLang.ParserCore;

namespace MarineLang.Inputs
{
    public class TokenInput : Input<Token>
    {
        public override RangePosition RangePosition => LastCurrent.rangePosition;

        public static TokenInput Create(Token[] tokens)
        {
            return new TokenInput(tokens);
        }

        TokenInput(Token[] tokens, int index = 0) : base(tokens, index)
        {
        }

        public override IInput<Token> Advance()
        {
            return new TokenInput(items, Index + 1);
        }
    }
}
