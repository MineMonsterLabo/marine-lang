using System.Collections.Generic;
using System.Linq;

namespace MarineLang.Models.Asts
{
    public abstract class ExprAst : StatementAst
    {
        public abstract ExprPriority ExprPriority { get; }

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

        public override IEnumerable<T> Accept<T>(AstVisitor<T> astVisitor)
        {
            return base.Accept(astVisitor).Append(astVisitor.Visit(this));
        }
    }

    public class ValueAst : ExprAst
    {
        public override Position Start => token.position;
        public override Position End => token.PositionEnd;
        public override ExprPriority ExprPriority => ExprPriority.Primary;

        public object value;
        public Token token;

        public static ValueAst Create(object value, Token token)
        {
            return new ValueAst { value = value, token = token };
        }

        public static ValueAst Create(object value)
        {
            return new ValueAst { value = value};
        }

        public override IEnumerable<T> Accept<T>(AstVisitor<T> astVisitor)
        {
            return base.Accept(astVisitor).Append(astVisitor.Visit(this));
        }

        public override IEnumerable<IAst> GetChildrenWithThis()
        {
            return Enumerable.Empty<IAst>().Append(this);
        }
    }

    public class VariableAst : ExprAst
    {
        public string VarName => varToken.text;
        public override Position Start => varToken.position;
        public override Position End => varToken.PositionEnd;
        public override ExprPriority ExprPriority => ExprPriority.Primary;

        public Token varToken;

        public static VariableAst Create(Token varToken)
        {
            return new VariableAst { varToken = varToken };
        }

        public static VariableAst Create(string name)
        {
            return new VariableAst { varToken = new Token(TokenType.Id, name) };
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

        public override IEnumerable<T> Accept<T>(AstVisitor<T> astVisitor)
        {
            return base.Accept(astVisitor).Append(astVisitor.Visit(this));
        }

        public override IEnumerable<IAst> GetChildrenWithThis()
        {
            return Enumerable.Empty<IAst>().Append(this);
        }
    }

    public class BinaryOpAst : ExprAst
    {
        public TokenType opKind;
        public ExprAst leftExpr;
        public ExprAst rightExpr;

        public override Position Start => leftExpr.Start;
        public override Position End => rightExpr.End;
        public override ExprPriority ExprPriority => ExprPriorityHelpr.GetBinaryOpPriority(opKind);

        public static BinaryOpAst Create(ExprAst leftExpr, ExprAst rightExpr, TokenType opKind)
        {
            return new BinaryOpAst
            {
                leftExpr = leftExpr,
                rightExpr = rightExpr,
                opKind = opKind
            };
        }

        public override IEnumerable<T> Accept<T>(AstVisitor<T> astVisitor)
        {
            return base.Accept(astVisitor).Append(astVisitor.Visit(this));
        }

        public override IEnumerable<IAst> GetChildrenWithThis()
        {
            return
                Enumerable.Empty<IAst>().Append(this)
                .Concat(leftExpr.GetChildrenWithThis())
                .Concat(rightExpr.GetChildrenWithThis());
        }
    }

    public class UnaryOpAst : ExprAst
    {
        public Token opToken;
        public ExprAst expr;

        public override Position Start => opToken.position;
        public override Position End => expr.End;
        public override ExprPriority ExprPriority => ExprPriority.Unary;

        public static UnaryOpAst Create(ExprAst expr, Token opToken)
        {
            return new UnaryOpAst
            {
                expr = expr,
                opToken = opToken
            };
        }

        public override IEnumerable<T> Accept<T>(AstVisitor<T> astVisitor)
        {
            return base.Accept(astVisitor).Append(astVisitor.Visit(this));
        }

        public override IEnumerable<IAst> GetChildrenWithThis()
        {
            return
                Enumerable.Empty<IAst>().Append(this)
                .Concat(expr.GetChildrenWithThis());
        }
    }

    public class InstanceFuncCallAst : ExprAst
    {
        public ExprAst instanceExpr;
        public FuncCallAst instancefuncCallAst;

        public override Position Start => instanceExpr.Start;
        public override Position End => instancefuncCallAst.End;
        public override ExprPriority ExprPriority => ExprPriority.Primary;

        public static InstanceFuncCallAst Create(ExprAst instanceExpr, FuncCallAst instancefuncCallAst)
        {
            return new InstanceFuncCallAst
            {
                instanceExpr = instanceExpr,
                instancefuncCallAst = instancefuncCallAst
            };
        }

        public override IEnumerable<T> Accept<T>(AstVisitor<T> astVisitor)
        {
            return base.Accept(astVisitor).Append(astVisitor.Visit(this));
        }

        public override IEnumerable<IAst> GetChildrenWithThis()
        {
            return
                Enumerable.Empty<IAst>().Append(this)
                .Concat(instanceExpr.GetChildrenWithThis())
                .Concat(instancefuncCallAst.GetChildrenWithThis());
        }
    }

    public class InstanceFieldAst : ExprAst
    {
        public ExprAst instanceExpr;
        public VariableAst variableAst;

        public override Position Start => instanceExpr.Start;
        public override Position End => variableAst.End;
        public override ExprPriority ExprPriority => ExprPriority.Primary;

        public static InstanceFieldAst Create(ExprAst instanceExpr, VariableAst variableAst)
        {
            return new InstanceFieldAst
            {
                instanceExpr = instanceExpr,
                variableAst = variableAst
            };
        }

        public override IEnumerable<T> Accept<T>(AstVisitor<T> astVisitor)
        {
            return base.Accept(astVisitor).Append(astVisitor.Visit(this));
        }

        public override IEnumerable<IAst> GetChildrenWithThis()
        {
            return
                Enumerable.Empty<IAst>().Append(this)
                .Concat(instanceExpr.GetChildrenWithThis())
                .Concat(variableAst.GetChildrenWithThis());
        }
    }

    public class AwaitAst : ExprAst
    {
        public Token awaitToken;
        public ExprAst instanceExpr;

        public override Position Start => awaitToken.position;
        public override Position End => instanceExpr.End;
        public override ExprPriority ExprPriority => ExprPriority.Primary;

        public static AwaitAst Create(Token awaitToken, ExprAst instanceExpr)
        {
            return new AwaitAst
            {
                awaitToken = awaitToken,
                instanceExpr = instanceExpr,
            };
        }

        public static AwaitAst Create(ExprAst instanceExpr)
        {
            return new AwaitAst
            {
                instanceExpr = instanceExpr,
            };
        }

        public override IEnumerable<T> Accept<T>(AstVisitor<T> astVisitor)
        {
            return base.Accept(astVisitor).Append(astVisitor.Visit(this));
        }

        public override IEnumerable<IAst> GetChildrenWithThis()
        {
            return
                Enumerable.Empty<IAst>().Append(this)
                .Concat(instanceExpr.GetChildrenWithThis());
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
        public override ExprPriority ExprPriority => ExprPriority.Primary;

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

        public static IfExprAst Create(
            ExprAst conditionExpr,
            StatementAst[] thenStatements,
            StatementAst[] elseStatements
        )
        {
            return new IfExprAst
            {
                conditionExpr = conditionExpr,
                thenStatements = thenStatements,
                elseStatements = elseStatements,
            };
        }

        public override IEnumerable<T> Accept<T>(AstVisitor<T> astVisitor)
        {
            return base.Accept(astVisitor).Append(astVisitor.Visit(this));
        }

        public override IEnumerable<IAst> GetChildrenWithThis()
        {
            return
                Enumerable.Empty<IAst>().Append(this)
                .Concat(conditionExpr.GetChildrenWithThis())
                .Concat(thenStatements.SelectMany(x => x.GetChildrenWithThis()))
                .Concat(elseStatements.SelectMany(x => x.GetChildrenWithThis()));
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
        public override ExprPriority ExprPriority => ExprPriority.Primary;

        public static GetIndexerAst Create(ExprAst expr, ExprAst indexExpr, Token rightBracketToken)
        {
            return new GetIndexerAst
            {
                instanceExpr = expr,
                indexExpr = indexExpr,
                rightBracketToken = rightBracketToken
            };
        }

        public static GetIndexerAst Create(ExprAst expr, ExprAst indexExpr)
        {
            return new GetIndexerAst
            {
                instanceExpr = expr,
                indexExpr = indexExpr,
            };
        }

        public override IEnumerable<T> Accept<T>(AstVisitor<T> astVisitor)
        {
            return base.Accept(astVisitor).Append(astVisitor.Visit(this));
        }

        public override IEnumerable<IAst> GetChildrenWithThis()
        {
            return
                Enumerable.Empty<IAst>().Append(this)
                .Concat(instanceExpr.GetChildrenWithThis())
                .Concat(indexExpr.GetChildrenWithThis());
        }
    }

    public class ArrayLiteralAst : ExprAst
    {
        public Token leftBracketToken;
        public Token rightBracketToken;
        public ArrayLiteralExprs arrayLiteralExprs;

        public override Position Start => leftBracketToken.position;
        public override Position End => rightBracketToken.PositionEnd;
        public override ExprPriority ExprPriority => ExprPriority.Primary;

        public class ArrayLiteralExprs
        {
            public ExprAst[] exprAsts;
            public int? size;
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

        public static ArrayLiteralAst Create(ArrayLiteralExprs arrayLiteralExprs)
        {
            return new ArrayLiteralAst
            {
                arrayLiteralExprs = arrayLiteralExprs,
            };
        }

        public override IEnumerable<T> Accept<T>(AstVisitor<T> astVisitor)
        {
            return base.Accept(astVisitor).Append(astVisitor.Visit(this));
        }

        public override IEnumerable<IAst> GetChildrenWithThis()
        {
            return
                Enumerable.Empty<IAst>().Append(this)
                .Concat(arrayLiteralExprs.exprAsts.SelectMany(x => x.GetChildrenWithThis()));
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
        public override ExprPriority ExprPriority => ExprPriority.Primary;

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

        public static ActionAst Create(VariableAst[] args, StatementAst[] statementAsts)
        {
            return new ActionAst
            {
                args = args,
                statementAsts = statementAsts,
            };
        }

        public override IEnumerable<T> Accept<T>(AstVisitor<T> astVisitor)
        {
            return base.Accept(astVisitor).Append(astVisitor.Visit(this));
        }

        public override IEnumerable<IAst> GetChildrenWithThis()
        {
            return
                Enumerable.Empty<IAst>().Append(this)
                .Concat(args.SelectMany(x => x.GetChildrenWithThis()))
                .Concat(statementAsts.SelectMany(x => x.GetChildrenWithThis()));
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
        public override ExprPriority ExprPriority => ExprPriority.Primary;

        public static FuncCallAst Create(Token funcNameToken, ExprAst[] args, Token rightParen)
        {
            return new FuncCallAst { funcNameToken = funcNameToken, args = args, rightParen = rightParen };
        }

        public static FuncCallAst Create(string funcName, ExprAst[] args)
        {
            return new FuncCallAst { funcNameToken = new Token(TokenType.Id, funcName), args = args };
        }

        public override IEnumerable<T> Accept<T>(AstVisitor<T> astVisitor)
        {
            return base.Accept(astVisitor).Append(astVisitor.Visit(this));
        }

        public override IEnumerable<IAst> GetChildrenWithThis()
        {
            return
                Enumerable.Empty<IAst>().Append(this)
                .Concat(args.SelectMany(x => x.GetChildrenWithThis()));
        }
    }
}
