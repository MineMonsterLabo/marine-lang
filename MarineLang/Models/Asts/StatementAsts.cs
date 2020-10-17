using System.Collections.Generic;
using System.Linq;

namespace MarineLang.Models.Asts
{
    public abstract class StatementAst : IAst
    {
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
        public ExprAst expr;

        public static ReturnAst Create(ExprAst expr)
        {
            return new ReturnAst { expr = expr };
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

        public static AssignmentVariableAst Create(string varName, ExprAst expr)
        {
            return new AssignmentVariableAst { varName = varName, expr = expr };
        }

        public override IEnumerable<T> LookUp<T>()
        {
            return expr.LookUp<T>();
        }
    }

    public class ReAssignmentVariableAst : StatementAst
    {
        public ExprAst expr;
        public string varName;

        public static ReAssignmentVariableAst Create(string varName, ExprAst expr)
        {
            return new ReAssignmentVariableAst { varName = varName, expr = expr };
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
        public ExprAst conditionExpr;
        public StatementAst[] statements;

        public static WhileAst Create(ExprAst conditionExpr, StatementAst[] statements)
        {
            return new WhileAst { conditionExpr = conditionExpr, statements = statements };
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
        public VariableAst initVariable;
        public ExprAst initExpr;
        public ExprAst maxValueExpr;
        public ExprAst addValueExpr;
        public StatementAst[] statements;

        public static ForAst Create
        (VariableAst initVariable, ExprAst initExpr, ExprAst maxValueExpr, ExprAst addValueExpr, StatementAst[] statements)
        {
            return new ForAst
            {
                initVariable = initVariable,
                initExpr = initExpr,
                maxValueExpr = maxValueExpr,
                addValueExpr = addValueExpr,
                statements = statements
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

        public override IEnumerable<T> LookUp<T>()
        {
            return Enumerable.Empty<T>();
        }
    }
}
