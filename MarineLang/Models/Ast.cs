using System.Collections.Generic;
using System.Linq;

namespace MarineLang.Models
{

    public interface IAst
    {
        IEnumerable<T> LookUp<T>();
    }

    public class ProgramAst : IAst
    {
        public FuncDefinitionAst[] funcDefinitionAsts;

        public static ProgramAst Create(FuncDefinitionAst[] funcDefinitionAsts)
        {
            return new ProgramAst { funcDefinitionAsts = funcDefinitionAsts };
        }

        public IEnumerable<T> LookUp<T>()
        {
            return funcDefinitionAsts.SelectMany(x => x.LookUp<T>());
        }
    }

    public class FuncDefinitionAst : IAst
    {
        public string funcName;
        public VariableAst[] args;
        public StatementAst[] statementAsts;

        public static FuncDefinitionAst Create(string funcName, VariableAst[] args, StatementAst[] statementAsts)
        {
            return new FuncDefinitionAst { funcName = funcName, args = args, statementAsts = statementAsts };
        }

        public IEnumerable<T> LookUp<T>()
        {
            return
                args.SelectMany(x => x.LookUp<T>())
                .Concat(statementAsts.SelectMany(x => x.LookUp<T>()));
        }
    }

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
    }

    public class ValueAst : ExprAst
    {
        public object value;

        public static ValueAst Create(object value)
        {
            return new ValueAst { value = value };
        }

        public override IEnumerable<T> LookUp<T>()
        {
            if (this is T t)
                yield return t;
        }
    }

    public class VariableAst : ExprAst
    {
        public string varName;

        public static VariableAst Create(string varName)
        {
            return new VariableAst { varName = varName };
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
                return x.varName == y.varName;
            }

            public int GetHashCode(VariableAst obj)
            {
                return obj.varName.GetHashCode();
            }
        }
    }

    public class BinaryOpAst : ExprAst
    {
        public TokenType opKind;
        public ExprAst leftExpr;
        public ExprAst rightExpr;

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

    public class InstanceFuncCallAst : ExprAst
    {
        public ExprAst instanceExpr;
        public FuncCallAst instancefuncCallAst;

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
        public string fieldName;

        public static InstanceFieldAst Create(ExprAst instanceExpr, string fieldName)
        {
            return new InstanceFieldAst
            {
                instanceExpr = instanceExpr,
                fieldName = fieldName
            };
        }

        public override IEnumerable<T> LookUp<T>()
        {
            return instanceExpr.LookUp<T>();
        }
    }

    public class IfExprAst : ExprAst
    {
        public ExprAst conditionExpr;
        public StatementAst[] thenStatements;
        public StatementAst[] elseStatements;

        public static IfExprAst Create(ExprAst conditionExpr, StatementAst[] thenStatements, StatementAst[] elseStatements)
        {
            return new IfExprAst
            {
                conditionExpr = conditionExpr,
                thenStatements = thenStatements,
                elseStatements = elseStatements
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
        public ExprAst instanceExpr;
        public ExprAst indexExpr;

        public static GetIndexerAst Create(ExprAst expr, ExprAst indexExpr)
        {
            return new GetIndexerAst { instanceExpr = expr, indexExpr = indexExpr };
        }

        public override IEnumerable<T> LookUp<T>()
        {
            return instanceExpr.LookUp<T>().Concat(indexExpr.LookUp<T>());
        }
    }

    public class ArrayLiteralAst : ExprAst
    {
        public ExprAst[] exprAsts;
        public int size;

        public static ArrayLiteralAst Create(ExprAst[] exprAsts, int size)
        {
            return new ArrayLiteralAst { exprAsts = exprAsts, size = size };
        }

        public override IEnumerable<T> LookUp<T>()
        {
            return exprAsts.SelectMany(x => x.LookUp<T>());
        }
    }

    public class ActionAst : ExprAst
    {
        public VariableAst[] args;
        public StatementAst[] statementAsts;

        public static ActionAst Create(VariableAst[] args, StatementAst[] statementAsts)
        {
            return new ActionAst { args = args, statementAsts = statementAsts };
        }

        public override IEnumerable<T> LookUp<T>()
        {
            return
                args.SelectMany(x => x.LookUp<T>())
                .Concat(statementAsts.SelectMany(x => x.LookUp<T>()));
        }
    }

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

    public class FuncCallAst : ExprAst
    {
        public string funcName;
        public ExprAst[] args;

        public static FuncCallAst Create(string funcName, ExprAst[] args)
        {
            return new FuncCallAst { funcName = funcName, args = args };
        }

        public override IEnumerable<T> LookUp<T>()
        {
            return args.SelectMany(x => x.LookUp<T>());
        }
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
        public string fieldName;

        public static FieldAssignmentAst Create(string fieldName, ExprAst instanceExpr, ExprAst expr)
        {
            return new FieldAssignmentAst { fieldName = fieldName, instanceExpr = instanceExpr, expr = expr };
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
