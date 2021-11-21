namespace MarineLang.Models
{
    public enum TokenType
    {
        Func,
        Id,
        ClassName,
        MacroName,
        Await,
        Yield,
        Break,
        If,
        Else,
        LeftParen,
        RightParen,
        LeftBracket,
        RightBracket,
        LeftCurlyBracket,
        RightCurlyBracket,
        Comma,
        Semicolon,
        Let,
        PipeOp,
        DotOp,
        AssignmentOp,
        OrOp,
        AndOp,
        EqualOp,
        NotEqualOp,
        GreaterOp,
        GreaterEqualOp,
        LessOp,
        LessEqualOp,
        PlusOp,
        MinusOp,
        MulOp,
        DivOp,
        ModOp,
        NotOp,
        Int,
        Bool,
        Char,
        String,
        Float,
        End,
        Return,
        While,
        For,
        ForEach,
        In,
        TwoColon,
        Colon,
        Dollar,
        UnKnown,
    }

    public static class TokenTypeExtension
    {
        public static string GetText(this TokenType tokenType)
        {
            switch (tokenType)
            {
                case TokenType.Func:
                    return "fun";
                case TokenType.Await:
                    return "await";
                case TokenType.Yield:
                    return "yield";
                case TokenType.Break:
                    return "break";
                case TokenType.If:
                    return "if";
                case TokenType.Else:
                    return "else";
                case TokenType.LeftParen:
                    return "(";
                case TokenType.RightParen:
                    return ")";
                case TokenType.LeftBracket:
                    return "[";
                case TokenType.RightBracket:
                    return "]";
                case TokenType.LeftCurlyBracket:
                    return "{";
                case TokenType.RightCurlyBracket:
                    return "}";
                case TokenType.Comma:
                    return ",";
                case TokenType.Semicolon:
                    return ";";
                case TokenType.Let:
                    return "let";
                case TokenType.PipeOp:
                    return "|";
                case TokenType.DotOp:
                    return ".";
                case TokenType.AssignmentOp:
                    return "=";
                case TokenType.OrOp:
                    return "||";
                case TokenType.AndOp:
                    return "&&";
                case TokenType.EqualOp:
                    return "==";
                case TokenType.NotEqualOp:
                    return "!=";
                case TokenType.GreaterOp:
                    return ">";
                case TokenType.GreaterEqualOp:
                    return ">=";
                case TokenType.LessOp:
                    return "<";
                case TokenType.LessEqualOp:
                    return "<=";
                case TokenType.PlusOp:
                    return "+";
                case TokenType.MinusOp:
                    return "-";
                case TokenType.MulOp:
                    return "*";
                case TokenType.DivOp:
                    return "/";
                case TokenType.ModOp:
                    return "%";
                case TokenType.NotOp:
                    return "!";
                case TokenType.End:
                    return "end";
                case TokenType.Return:
                    return "ret";
                case TokenType.While:
                    return "while";
                case TokenType.For:
                    return "for";
                case TokenType.ForEach:
                    return "foreach";
                case TokenType.In:
                    return "in";
                case TokenType.TwoColon:
                    return "::";
                case TokenType.Colon:
                    return ":";
                case TokenType.Dollar:
                    return "$";
            }
            return null;
        }
    }
}
