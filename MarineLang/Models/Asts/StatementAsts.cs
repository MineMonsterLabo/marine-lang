using System.Collections.Generic;
using System.Linq;

namespace MarineLang.Models.Asts
{
    public abstract class StatementAst : IAst
    {
        public abstract Position Start { get; }
        public abstract Position End { get; }

        public ExprAst GetExprAst()
        {
            return this as ExprAst;
        }

        public ReturnAst GetReturnAst()
        {
            return this as ReturnAst;
        }

        public AssignmentVariableAst GetAssignmentAst()
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

        public abstract IEnumerable<T> LookUp<T>();
    }

    public class ReturnAst : StatementAst
    {
        public Token retToken;
        public ExprAst expr;

        public override Position Start => retToken.position;
        public override Position End => expr.End;

        public static ReturnAst Create(Token retToken, ExprAst expr)
        {
            return new ReturnAst { retToken = retToken, expr = expr };
        }

        public override IEnumerable<T> LookUp<T>()
        {
            return expr.LookUp<T>();
        }
    }

    public class AssignmentVariableAst : StatementAst
    {
        public ExprAst expr;
        public string varName;
        public Token letToken;

        public override Position Start => letToken.position;
        public override Position End => expr.End;

        public static AssignmentVariableAst Create(Token letToken, string varName, ExprAst expr)
        {
            return new AssignmentVariableAst { letToken = letToken, varName = varName, expr = expr };
        }

        public override IEnumerable<T> LookUp<T>()
        {
            return expr.LookUp<T>();
        }
    }

    public class ReAssignmentVariableAst : StatementAst
    {
        public Token varNameToken;
        public ExprAst expr;

        public override Position Start => varNameToken.position;
        public override Position End => expr.End;

        public static ReAssignmentVariableAst Create(Token varNameToken, ExprAst expr)
        {
            return new ReAssignmentVariableAst { varNameToken = varNameToken, expr = expr };
        }

        public override IEnumerable<T> LookUp<T>()
        {
            return expr.LookUp<T>();
        }
    }

    public class ReAssignmentIndexerAst : StatementAst
    {
        public ExprAst instanceExpr;
        public ExprAst indexExpr;
        public ExprAst assignmentExpr;

        public override Position Start => instanceExpr.Start;
        public override Position End => assignmentExpr.End;

        public static ReAssignmentIndexerAst Create(ExprAst instanceExpr, ExprAst indexExpr, ExprAst assignmentExpr)
        {
            return new ReAssignmentIndexerAst { instanceExpr = instanceExpr, indexExpr = indexExpr, assignmentExpr = assignmentExpr };
        }

        public override IEnumerable<T> LookUp<T>()
        {
            return
                instanceExpr.LookUp<T>()
                .Concat(indexExpr.LookUp<T>())
                .Concat(assignmentExpr.LookUp<T>());
        }
    }

    public class FieldAssignmentAst : StatementAst
    {
        public ExprAst expr;
        public ExprAst instanceExpr;
        public VariableAst variableAst;

        public override Position Start => instanceExpr.Start;
        public override Position End => expr.End;

        public static FieldAssignmentAst Create(VariableAst variableAst, ExprAst instanceExpr, ExprAst expr)
        {
            return new FieldAssignmentAst { variableAst = variableAst, instanceExpr = instanceExpr, expr = expr };
        }

        public override IEnumerable<T> LookUp<T>()
        {
            return
                expr.LookUp<T>()
                .Concat(instanceExpr.LookUp<T>());
        }
    }

    public class WhileAst : StatementAst
    {
        public Token whileToken;
        public ExprAst conditionExpr;
        public StatementAst[] statements;
        public Token endRightCurlyBracket;

        public override Position Start => whileToken.position;
        public override Position End => endRightCurlyBracket.PositionEnd;

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

        public override IEnumerable<T> LookUp<T>()
        {
            return
                conditionExpr.LookUp<T>()
                .Concat(statements.SelectMany(x_ => x_.LookUp<T>()));
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

        public override Position Start => forToken.position;
        public override Position End => endRightCurlyBracket.PositionEnd;

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

        public override IEnumerable<T> LookUp<T>()
        {
            return
                initVariable.LookUp<T>()
                .Concat(initExpr.LookUp<T>())
                .Concat(maxValueExpr.LookUp<T>())
                .Concat(addValueExpr.LookUp<T>())
                .Concat(statements.SelectMany(x => x.LookUp<T>()));
        }
    }

    public class YieldAst : StatementAst
    {
        public Token yieldToken;

        public override Position Start => yieldToken.position;
        public override Position End => yieldToken.PositionEnd;

        public static YieldAst Create(Token yieldToken)
        {
            return new YieldAst { yieldToken = yieldToken };
        }

        public override IEnumerable<T> LookUp<T>()
        {
            return Enumerable.Empty<T>();
        }
    }
}
