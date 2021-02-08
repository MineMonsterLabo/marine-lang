using System.Collections.Generic;
using System.Linq;

namespace MarineLang.Models.Asts
{
    public abstract class StatementAst : IAst
    {
        public abstract RangePosition Range { get; }

        public ExprStatementAst GetExprStatementAst()
        {
            return this as ExprStatementAst;
        }

        public ReturnAst GetReturnAst()
        {
            return this as ReturnAst;
        }

        public AssignmentVariableAst GetAssignmentVariableAst()
        {
            return this as AssignmentVariableAst;
        }

        public ReAssignmentVariableAst GetReAssignmentVariableAst()
        {
            return this as ReAssignmentVariableAst;
        }
        public ReAssignmentIndexerAst GetReAssignmentIndexerAst()
        {
            return this as ReAssignmentIndexerAst;
        }

        public FieldAssignmentAst GetFieldAssignmentAst()
        {
            return this as FieldAssignmentAst;
        }

        public WhileAst GetWhileAst()
        {
            return this as WhileAst;
        }

        public ForAst GetForAst()
        {
            return this as ForAst;
        }

        public YieldAst GetYieldAst()
        {
            return this as YieldAst;
        }

        public virtual IEnumerable<T> Accept<T>(AstVisitor<T> astVisitor)
        {
            return Enumerable.Empty<T>().Append(astVisitor.Visit(this));
        }

        public abstract IEnumerable<IAst> GetChildrenWithThis();
    }

    public class ExprStatementAst : StatementAst
    {
        public ExprAst expr;

        public override RangePosition Range => expr.Range;

        public static ExprStatementAst Create(ExprAst expr)
        {
            return new ExprStatementAst { expr = expr };
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

    public class ReturnAst : StatementAst
    {
        public Token retToken;
        public ExprAst expr;

        public override RangePosition Range => new RangePosition(retToken.position, expr.Range.End);

        public static ReturnAst Create(Token retToken, ExprAst expr)
        {
            return new ReturnAst { retToken = retToken, expr = expr };
        }

        public static ReturnAst Create(ExprAst expr)
        {
            return new ReturnAst { expr = expr };
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

    public class AssignmentVariableAst : StatementAst
    {
        public ExprAst expr;
        public VariableAst variableAst;
        public Token letToken;

        public override RangePosition Range => new RangePosition(letToken.position, expr.Range.End);

        public static AssignmentVariableAst Create(Token letToken, VariableAst variableAst, ExprAst expr)
        {
            return new AssignmentVariableAst { letToken = letToken, variableAst = variableAst, expr = expr };
        }

        public static AssignmentVariableAst Create(VariableAst variableAst, ExprAst expr)
        {
            return new AssignmentVariableAst { variableAst = variableAst, expr = expr };
        }

        public override IEnumerable<T> Accept<T>(AstVisitor<T> astVisitor)
        {
            return base.Accept(astVisitor).Append(astVisitor.Visit(this));
        }

        public override IEnumerable<IAst> GetChildrenWithThis()
        {
            return
                Enumerable.Empty<IAst>().Append(this)
                .Concat(variableAst.GetChildrenWithThis())
                .Concat(expr.GetChildrenWithThis());
        }
    }

    public class ReAssignmentVariableAst : StatementAst
    {
        public VariableAst variableAst;
        public ExprAst expr;

        public override RangePosition Range => new RangePosition(variableAst.Range.Start, expr.Range.End);

        public static ReAssignmentVariableAst Create(VariableAst variableAst, ExprAst expr)
        {
            return new ReAssignmentVariableAst { variableAst = variableAst, expr = expr };
        }

        public override IEnumerable<T> Accept<T>(AstVisitor<T> astVisitor)
        {
            return base.Accept(astVisitor).Append(astVisitor.Visit(this));
        }

        public override IEnumerable<IAst> GetChildrenWithThis()
        {
            return
                Enumerable.Empty<IAst>().Append(this)
                .Concat(variableAst.GetChildrenWithThis())
                .Concat(expr.GetChildrenWithThis());
        }
    }

    public class ReAssignmentIndexerAst : StatementAst
    {
        public GetIndexerAst getIndexerAst;
        public ExprAst assignmentExpr;

        public override RangePosition Range
            => new RangePosition(getIndexerAst.Range.Start, assignmentExpr.Range.End);

        public static ReAssignmentIndexerAst Create(GetIndexerAst getIndexerAst, ExprAst assignmentExpr)
        {
            return new ReAssignmentIndexerAst { getIndexerAst = getIndexerAst, assignmentExpr = assignmentExpr };
        }

        public override IEnumerable<T> Accept<T>(AstVisitor<T> astVisitor)
        {
            return base.Accept(astVisitor).Append(astVisitor.Visit(this));
        }

        public override IEnumerable<IAst> GetChildrenWithThis()
        {
            return
                Enumerable.Empty<IAst>().Append(this)
                .Concat(getIndexerAst.GetChildrenWithThis())
                .Concat(assignmentExpr.GetChildrenWithThis());
        }
    }

    public class FieldAssignmentAst : StatementAst
    {
        public ExprAst expr;
        public InstanceFieldAst instanceFieldAst;

        public override RangePosition Range => new RangePosition(instanceFieldAst.Range.Start, expr.Range.End);

        public static FieldAssignmentAst Create(InstanceFieldAst instanceFieldAst, ExprAst expr)
        {
            return new FieldAssignmentAst { instanceFieldAst = instanceFieldAst, expr = expr };
        }

        public override IEnumerable<T> Accept<T>(AstVisitor<T> astVisitor)
        {
            return base.Accept(astVisitor).Append(astVisitor.Visit(this));
        }

        public override IEnumerable<IAst> GetChildrenWithThis()
        {
            return
                Enumerable.Empty<IAst>().Append(this)
                .Concat(instanceFieldAst.GetChildrenWithThis())
                .Concat(expr.GetChildrenWithThis());
        }
    }

    public class WhileAst : StatementAst
    {
        public Token whileToken;
        public ExprAst conditionExpr;
        public StatementAst[] statements;
        public Token endRightCurlyBracket;

        public override RangePosition Range => new RangePosition(whileToken.position, endRightCurlyBracket.PositionEnd);

        public static WhileAst Create(Token whileToken, ExprAst conditionExpr, StatementAst[] statements, Token endRightCurlyBracket)
        {
            return new WhileAst
            {
                whileToken = whileToken,
                conditionExpr = conditionExpr,
                statements = statements,
                endRightCurlyBracket = endRightCurlyBracket
            };
        }

        public static WhileAst Create(ExprAst conditionExpr, StatementAst[] statements)
        {
            return new WhileAst
            {
                conditionExpr = conditionExpr,
                statements = statements,
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
                .Concat(statements.SelectMany(x => x.GetChildrenWithThis()));
        }
    }

    public class ForAst : StatementAst
    {
        public Token forToken;
        public VariableAst initVariable;
        public ExprAst initExpr;
        public ExprAst maxValueExpr;
        public ExprAst addValueExpr;
        public StatementAst[] statements;
        public Token endRightCurlyBracket;

        public override RangePosition Range
            => new RangePosition(forToken.position, endRightCurlyBracket.PositionEnd);

        public static ForAst Create(
            Token forToken,
            VariableAst initVariable,
            ExprAst initExpr,
            ExprAst maxValueExpr,
            ExprAst addValueExpr,
            StatementAst[] statements,
            Token endRightCurlyBracket
        )
        {
            return new ForAst
            {
                forToken = forToken,
                initVariable = initVariable,
                initExpr = initExpr,
                maxValueExpr = maxValueExpr,
                addValueExpr = addValueExpr,
                statements = statements,
                endRightCurlyBracket = endRightCurlyBracket
            };
        }

        public static ForAst Create(
           VariableAst initVariable,
           ExprAst initExpr,
           ExprAst maxValueExpr,
           ExprAst addValueExpr,
           StatementAst[] statements
       )
        {
            return new ForAst
            {
                initVariable = initVariable,
                initExpr = initExpr,
                maxValueExpr = maxValueExpr,
                addValueExpr = addValueExpr,
                statements = statements,
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
                .Concat(initVariable.GetChildrenWithThis())
                .Concat(initExpr.GetChildrenWithThis())
                .Concat(maxValueExpr.GetChildrenWithThis())
                .Concat(addValueExpr.GetChildrenWithThis())
                .Concat(statements.SelectMany(x => x.GetChildrenWithThis()));
        }
    }

    public class YieldAst : StatementAst
    {
        public Token yieldToken;

        public override RangePosition Range => new RangePosition(yieldToken.position, yieldToken.PositionEnd);

        public static YieldAst Create(Token yieldToken)
        {
            return new YieldAst { yieldToken = yieldToken };
        }

        public static YieldAst Create()
        {
            return new YieldAst { };
        }

        public override IEnumerable<T> Accept<T>(AstVisitor<T> astVisitor)
        {
            return base.Accept(astVisitor).Append(astVisitor.Visit(this));
        }

        public override IEnumerable<IAst> GetChildrenWithThis()
        {
            return
                Enumerable.Empty<IAst>().Append(this);
        }
    }
}
