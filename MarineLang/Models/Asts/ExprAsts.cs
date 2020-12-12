using System.Collections.Generic;
using System.Linq;

namespace MarineLang.Models.Asts
{
    public abstract class ExprAst : StatementAst
    {
        public FuncCallAst GetFuncCallAst()
        {
            return this as FuncCallAst;
        }

        public ValueAst GetValueAst()
        {
            return this as ValueAst;
        }

        public VariableAst GetVariableAst()
        {
            return this as VariableAst;
        }

        public BinaryOpAst GetBinaryOpAst()
        {
            return this as BinaryOpAst;
        }

        public IfExprAst GetIfExprAst()
        {
            return this as IfExprAst;
        }

        public InstanceFuncCallAst GetInstanceFuncCallAst()
        {
            return this as InstanceFuncCallAst;
        }

        public InstanceFieldAst GetInstanceFieldAst()
        {
            return this as InstanceFieldAst;
        }

        public GetIndexerAst GetGetIndexerAst()
        {
            return this as GetIndexerAst;
        }

        public ArrayLiteralAst GetArrayLiteralAst()
        {
            return this as ArrayLiteralAst;
        }

        public ActionAst GetActionAst()
        {
            return this as ActionAst;
        }

        public AwaitAst GetAwaitAst()
        {
            return this as AwaitAst;
        }

        public UnaryOpAst GetUnaryOpAst()
        {
            return this as UnaryOpAst;
        }
    }

    public class ValueAst : ExprAst
    {
        public override Position Start => token.position;
        public override Position End => token.PositionEnd;

        public object value;
        public Token token;

        public static ValueAst Create(object value, Token token)
        {
            return new ValueAst { value = value, token = token };
        }

        public override IEnumerable<T> LookUp<T>()
        {
            if (this is T t)
                yield return t;
        }
    }

    public class VariableAst : ExprAst
    {
        public string VarName => varToken.text;
        public override Position Start => varToken.position;
        public override Position End => varToken.PositionEnd;

        public Token varToken;

        public static VariableAst Create(Token varToken)
        {
            return new VariableAst { varToken = varToken };
        }

        public override IEnumerable<T> LookUp<T>()
        {
            if (this is T t)
                yield return t;
        }

        public class Comparer : IEqualityComparer<VariableAst>
        {
            public bool Equals(VariableAst x, VariableAst y)
            {
                return x.VarName == y.VarName;
            }

            public int GetHashCode(VariableAst obj)
            {
                return obj.VarName.GetHashCode();
            }
        }
    }

    public class BinaryOpAst : ExprAst
    {
        public TokenType opKind;
        public ExprAst leftExpr;
        public ExprAst rightExpr;

        public override Position Start => leftExpr.Start;
        public override Position End => rightExpr.End;

        public static BinaryOpAst Create(ExprAst leftExpr, ExprAst rightExpr, TokenType opKind)
        {
            return new BinaryOpAst
            {
                leftExpr = leftExpr,
                rightExpr = rightExpr,
                opKind = opKind
            };
        }

        public override IEnumerable<T> LookUp<T>()
        {
            return leftExpr.LookUp<T>().Concat(rightExpr.LookUp<T>());
        }
    }

    public class UnaryOpAst : ExprAst
    {
        public Token opToken;
        public ExprAst expr;

        public override Position Start => opToken.position;
        public override Position End => expr.End;

        public static UnaryOpAst Create(ExprAst expr, Token opToken)
        {
            return new UnaryOpAst
            {
                expr = expr,
                opToken = opToken
            };
        }

        public override IEnumerable<T> LookUp<T>()
        {
            return expr.LookUp<T>();
        }
    }

    public class InstanceFuncCallAst : ExprAst
    {
        public ExprAst instanceExpr;
        public FuncCallAst instancefuncCallAst;

        public override Position Start => instanceExpr.Start;
        public override Position End => instancefuncCallAst.End;

        public static InstanceFuncCallAst Create(ExprAst instanceExpr, FuncCallAst instancefuncCallAst)
        {
            return new InstanceFuncCallAst
            {
                instanceExpr = instanceExpr,
                instancefuncCallAst = instancefuncCallAst
            };
        }

        public override IEnumerable<T> LookUp<T>()
        {
            return instanceExpr.LookUp<T>().Concat(instancefuncCallAst.LookUp<T>());
        }
    }

    public class InstanceFieldAst : ExprAst
    {
        public ExprAst instanceExpr;
        public VariableAst variableAst;

        public override Position Start => instanceExpr.Start;
        public override Position End => variableAst.End;

        public static InstanceFieldAst Create(ExprAst instanceExpr, VariableAst variableAst)
        {
            return new InstanceFieldAst
            {
                instanceExpr = instanceExpr,
                variableAst = variableAst
            };
        }

        public override IEnumerable<T> LookUp<T>()
        {
            return instanceExpr.LookUp<T>();
        }
    }

    public class AwaitAst : ExprAst
    {
        public Token awaitToken;
        public ExprAst instanceExpr;

        public override Position Start => awaitToken.position;
        public override Position End => instanceExpr.End;

        public static AwaitAst Create(Token awaitToken, ExprAst instanceExpr)
        {
            return new AwaitAst
            {
                awaitToken = awaitToken,
                instanceExpr = instanceExpr,
            };
        }

        public override IEnumerable<T> LookUp<T>()
        {
            return instanceExpr.LookUp<T>();
        }
    }

    public class IfExprAst : ExprAst
    {
        Token ifToken;
        public ExprAst conditionExpr;
        public StatementAst[] thenStatements;
        public StatementAst[] elseStatements;
        public Token endRightCurlyBracket;

        public override Position Start => ifToken.position;
        public override Position End => endRightCurlyBracket.PositionEnd;

        public static IfExprAst Create(
            Token ifToken,
            ExprAst conditionExpr,
            StatementAst[] thenStatements,
            StatementAst[] elseStatements,
            Token endRightCurlyBracket
        )
        {
            return new IfExprAst
            {
                ifToken = ifToken,
                conditionExpr = conditionExpr,
                thenStatements = thenStatements,
                elseStatements = elseStatements,
                endRightCurlyBracket = endRightCurlyBracket
            };
        }

        public override IEnumerable<T> LookUp<T>()
        {
            return
                conditionExpr.LookUp<T>()
                .Concat(thenStatements.SelectMany(x => x.LookUp<T>()))
                .Concat(elseStatements.SelectMany(x => x.LookUp<T>()));
        }
    }

    public class GetIndexerAst : ExprAst
    {
        public Token leftBracketToken;
        public Token rightBracketToken;
        public ExprAst instanceExpr;
        public ExprAst indexExpr;

        public override Position Start => instanceExpr.Start;
        public override Position End => rightBracketToken.PositionEnd;

        public static GetIndexerAst Create(ExprAst expr, ExprAst indexExpr, Token rightBracketToken)
        {
            return new GetIndexerAst
            {
                instanceExpr = expr,
                indexExpr = indexExpr,
                rightBracketToken = rightBracketToken
            };
        }

        public override IEnumerable<T> LookUp<T>()
        {
            return instanceExpr.LookUp<T>().Concat(indexExpr.LookUp<T>());
        }
    }

    public class ArrayLiteralAst : ExprAst
    {
        public Token leftBracketToken;
        public Token rightBracketToken;
        public ArrayLiteralExprs arrayLiteralExprs;

        public override Position Start => leftBracketToken.position;
        public override Position End => rightBracketToken.PositionEnd;

        public class ArrayLiteralExprs
        {
            public ExprAst[] exprAsts;
            public int size;
        }

        public static ArrayLiteralAst Create(Token leftBracketToken, ArrayLiteralExprs arrayLiteralExprs, Token rightBracketToken)
        {
            return new ArrayLiteralAst
            {
                leftBracketToken = leftBracketToken,
                arrayLiteralExprs = arrayLiteralExprs,
                rightBracketToken = rightBracketToken
            };
        }

        public override IEnumerable<T> LookUp<T>()
        {
            return arrayLiteralExprs.exprAsts.SelectMany(x => x.LookUp<T>());
        }
    }

    public class ActionAst : ExprAst
    {
        public VariableAst[] args;
        public StatementAst[] statementAsts;
        public Token leftCurlyBracketToken;
        public Token rightCurlyBracketToken;

        public override Position Start => leftCurlyBracketToken.position;
        public override Position End => rightCurlyBracketToken.PositionEnd;

        public static ActionAst Create(Token leftCurlyBracketToken, VariableAst[] args, StatementAst[] statementAsts, Token rightCurlyBracketToken)
        {
            return new ActionAst
            {
                leftCurlyBracketToken = leftCurlyBracketToken,
                args = args,
                statementAsts = statementAsts,
                rightCurlyBracketToken = rightCurlyBracketToken
            };
        }

        public override IEnumerable<T> LookUp<T>()
        {
            return
                args.SelectMany(x => x.LookUp<T>())
                .Concat(statementAsts.SelectMany(x => x.LookUp<T>()));
        }
    }

    public class FuncCallAst : ExprAst
    {
        public Token funcNameToken;
        public ExprAst[] args;
        public Token rightParen;

        public string FuncName => funcNameToken.text;
        public override Position Start => funcNameToken.position;
        public override Position End => rightParen.PositionEnd;

        public static FuncCallAst Create(Token funcNameToken, ExprAst[] args, Token rightParen)
        {
            return new FuncCallAst { funcNameToken = funcNameToken, args = args, rightParen = rightParen };
        }

        public override IEnumerable<T> LookUp<T>()
        {
            return args.SelectMany(x => x.LookUp<T>());
        }
    }
}
