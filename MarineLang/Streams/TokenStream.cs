using MarineLang.Models;
using MarineLang.ParserCore;

namespace MarineLang.Streams
{
    public class TokenStream : Stream<Token>
    {
        public override RangePosition RangePosition => LastCurrent.rangePosition;

        public static TokenStream Create(Token[] tokens)
        {
            return new TokenStream(tokens);
        }

        TokenStream(Token[] tokens, int index = 0) : base(tokens, index)
        {
        }

        public override IInput<Token> Advance()
        {
            return new TokenStream(items, Index + 1);
        }
    }
}
