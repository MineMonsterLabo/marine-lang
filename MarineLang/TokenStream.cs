namespace MarineLang
{
    public class TokenStream : Stream<Token>
    {
        public static TokenStream Create(Token[] tokens)
        {
            return new TokenStream(tokens);
        }

        TokenStream(Token[] tokens) : base(tokens)
        {
        }
    }
}
