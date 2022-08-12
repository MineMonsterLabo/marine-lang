using System.Collections.Generic;
using System.Linq;

namespace MarineLang.Models.Asts
{
    public abstract class ExprAst : IAst
    {
        public abstract RangePosition Range { get; }
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

        public StaticFuncCallAst GetStaticFuncCallAst()
        {
            return this as StaticFuncCallAst;
        }

        public InstanceFieldAst GetInstanceFieldAst()
        {
            return this as InstanceFieldAst;
        }

        public StaticFieldAst GetStaticFieldAst()
        {
            return this as StaticFieldAst;
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

        public DictionaryConstructAst GetDictionaryConstructAst()
        {
            return this as DictionaryConstructAst;
        }

        public ErrorExprAst GetErrorExprAst()
        {
            return this as ErrorExprAst;
        }

        public virtual IEnumerable<T> Accept<T>(AstVisitor<T> astVisitor)
        {
            return Enumerable.Empty<T>().Append(astVisitor.Visit(this));
        }

        public abstract IEnumerable<IAst> GetChildrenWithThis();
    }

    public class ErrorExprAst : ExprAst
    {
        public override RangePosition Range => new RangePosition();
        public override ExprPriority ExprPriority => ExprPriority.Primary;

        public static ErrorExprAst Create()
        {
            return new ErrorExprAst();
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

    public class ValueAst : ExprAst
    {
        public override RangePosition Range =>
            token == null ? new RangePosition() : new RangePosition(token.position, token.PositionEnd);
        public override ExprPriority ExprPriority => ExprPriority.Primary;

        public object value;
        public Token token;

        public static ValueAst Create(object value, Token token)
        {
            return new ValueAst { value = value, token = token };
        }

        public static ValueAst Create(object value)
        {
            return new ValueAst { value = value };
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
        public override RangePosition Range => new RangePosition(varToken.position, varToken.PositionEnd);
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

        public override RangePosition Range => new RangePosition(leftExpr.Range.Start, rightExpr.Range.End);
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

        public override RangePosition Range => new RangePosition(opToken.position, expr.Range.End);
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

        public override RangePosition Range => new RangePosition(instanceExpr.Range.Start, instancefuncCallAst.Range.End);
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

    public class StaticFuncCallAst : ExprAst
    {
        public Token classNameToken;
        public FuncCallAst funcCallAst;
        public string ClassName => classNameToken.text;


        public override RangePosition Range => new RangePosition(classNameToken.position, funcCallAst.Range.End);
        public override ExprPriority ExprPriority => ExprPriority.Primary;

        public static StaticFuncCallAst Create(Token classNameToken, FuncCallAst funcCallAst)
        {
            return new StaticFuncCallAst
            {
                classNameToken = classNameToken,
                funcCallAst = funcCallAst
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
                .Concat(funcCallAst.GetChildrenWithThis());
        }
    }

    public class InstanceFieldAst : ExprAst
    {
        public ExprAst instanceExpr;
        public VariableAst variableAst;

        public override RangePosition Range => new RangePosition(instanceExpr.Range.Start, variableAst.Range.End);
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

    public class StaticFieldAst : ExprAst
    {
        public VariableAst variableAst;
        public Token classNameToken;
        public string ClassName => classNameToken.text;

        public override RangePosition Range => new RangePosition(classNameToken.position, variableAst.Range.End);
        public override ExprPriority ExprPriority => ExprPriority.Primary;

        public static StaticFieldAst Create(Token classNameToken, VariableAst variableAst)
        {
            return new StaticFieldAst
            {
                variableAst = variableAst,
                classNameToken = classNameToken
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
                .Concat(variableAst.GetChildrenWithThis());
        }
    }

    public class AwaitAst : ExprAst
    {
        public Token awaitToken;
        public ExprAst instanceExpr;

        public override RangePosition Range => new RangePosition(awaitToken.position, instanceExpr.Range.End);
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

        public override RangePosition Range => new RangePosition(ifToken.position, endRightCurlyBracket.PositionEnd);
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

        public override RangePosition Range => new RangePosition(instanceExpr.Range.Start, rightBracketToken.PositionEnd);
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

        public override RangePosition Range => new RangePosition(leftBracketToken.position, rightBracketToken.PositionEnd);
        public override ExprPriority ExprPriority => ExprPriority.Primary;

        public class ArrayLiteralExprs
        {
            public ExprAst[] exprAsts;
            public int? size;

            public static ArrayLiteralExprs Create(ExprAst[] exprAsts)
            {
                return new ArrayLiteralExprs { exprAsts = exprAsts };
            }

            public static ArrayLiteralExprs Create(ExprAst[] exprAsts, int? size)
            {
                return new ArrayLiteralExprs { exprAsts = exprAsts, size = size };
            }
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

        public override RangePosition Range => new RangePosition(leftCurlyBracketToken.position, rightCurlyBracketToken.PositionEnd);
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
        public Token[] namespaceTokens;
        public Token funcNameToken;
        public ExprAst[] args;
        public Token rightParen;

        public string FuncName => funcNameToken.text;
        public IEnumerable<string> NamespaceSettings
            => namespaceTokens
            .Where((token, index) => index != 0 || token.text != "global")
            .Select(token => token.text);

        public override RangePosition Range
            => rightParen == null ? new RangePosition() : new RangePosition(StartPosition, rightParen.PositionEnd);
        public override ExprPriority ExprPriority => ExprPriority.Primary;

        private Position StartPosition => namespaceTokens.DefaultIfEmpty(funcNameToken).First().position;

        public static FuncCallAst Create(Token[] namespaceTokens, Token funcNameToken, ExprAst[] args, Token rightParen)
        {
            return new FuncCallAst
            {
                namespaceTokens = namespaceTokens,
                funcNameToken = funcNameToken,
                args = args,
                rightParen = rightParen
            };
        }

        public static FuncCallAst Create(Token[] namespaceTokens, string funcName, ExprAst[] args)
        {
            return new FuncCallAst
            {
                namespaceTokens = namespaceTokens,
                funcNameToken = new Token(TokenType.Id, funcName),
                args = args
            };
        }

        public static FuncCallAst Create(string funcName, ExprAst[] args)
        {
            return Create(new Token[] { }, funcName, args);
        }

        public static FuncCallAst Create(Token funcNameToken, ExprAst[] args, Token rightParen)
        {
            return Create(new Token[] { }, funcNameToken, args,rightParen);
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

    public class DictionaryConstructAst : ExprAst
    {
        public Dictionary<string,ExprAst> dict;
        public Token start;
        public Token end;
        public override RangePosition Range => new RangePosition(start.position, end.PositionEnd);
        public override ExprPriority ExprPriority => ExprPriority.Primary;

        public static DictionaryConstructAst Create(Token start, Token end, Dictionary<string, ExprAst> dict)
        {
            return new DictionaryConstructAst
            {
                dict = dict,
                start = start,
                end = end
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
                .Concat(dict.Values.SelectMany(x => x.GetChildrenWithThis()));
        }
    }
}
