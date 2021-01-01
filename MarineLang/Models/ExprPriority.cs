namespace MarineLang.Models
{
    public enum ExprPriority
    {
        Or = 0,
        And,
        Equality,
        Relational,
        Additive,
        Multiplicative,
        Unary,
        Primary,
        Other
    }

    public static class ExprPriorityHelpr
    {
        public static ExprPriority GetBinaryOpPriority(TokenType tokenType)
        {
            switch (tokenType)
            {
                case TokenType.OrOp:
                    return ExprPriority.Or;

                case TokenType.AndOp:
                    return ExprPriority.And;

                case TokenType.EqualOp:
                case TokenType.NotEqualOp:
                    return ExprPriority.Equality;

                case TokenType.GreaterOp:
                case TokenType.GreaterEqualOp:
                case TokenType.LessOp:
                case TokenType.LessEqualOp:
                    return ExprPriority.Relational;

                case TokenType.PlusOp:
                case TokenType.MinusOp:
                    return ExprPriority.Additive;

                case TokenType.MulOp:
                case TokenType.DivOp:
                case TokenType.ModOp:
                    return ExprPriority.Multiplicative;
            }

            return ExprPriority.Other;
        }
    }
}
