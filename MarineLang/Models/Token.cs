namespace MarineLang.Models
{
    public class Token
    {
        public readonly TokenType tokenType;
        public readonly string text;
        public readonly Position position;
        public Position PositionEnd => new Position(position.line, position.column + text.Length);
        public RangePosition rangePosition => new RangePosition(position, PositionEnd);

        public Token(TokenType tokenType, string text, Position position)
        {
            this.tokenType = tokenType;
            this.text = text;
            this.position = position;
        }
    }
}
