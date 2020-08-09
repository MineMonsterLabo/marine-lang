namespace MarineLang.Models
{
    public class Token
    {
        public readonly TokenType tokenType;
        public readonly string text;
        public readonly int begin;
        public readonly int end;

        public Token(TokenType tokenType, string text, int begin, int end)
        {
            this.tokenType = tokenType;
            this.text = text;
            this.begin = begin;
            this.end = end;
        }
    }
}
