namespace MarineLang.Models
{
    public class Token
    {
        public readonly TokenType tokenType;
        public readonly string text;
        public readonly Position position;

        public Token(TokenType tokenType, string text, Position position)
        {
            this.tokenType = tokenType;
            this.text = text;
            this.position = position;
        }
    }
}
