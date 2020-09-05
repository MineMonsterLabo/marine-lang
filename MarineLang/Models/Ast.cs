namespace MarineLang.Models
{
    public class ProgramAst
    {
        public FuncDefinitionAst[] funcDefinitionAsts;

        public static ProgramAst Create(FuncDefinitionAst[] funcDefinitionAsts)
        {
            return new ProgramAst { funcDefinitionAsts = funcDefinitionAsts };
        }
    }

    public class FuncDefinitionAst
    {
        public string funcName;
        public VariableAst[] args;
        public StatementAst[] statementAsts;

        public static FuncDefinitionAst Create(string funcName, VariableAst[] args, StatementAst[] statementAsts)
        {
            return new FuncDefinitionAst { funcName = funcName, args = args, statementAsts = statementAsts };
        }
    }

    public class ExprAst : StatementAst
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
    }

    public class ValueAst : ExprAst
    {
        public object value;

        public static ValueAst Create(object value)
        {
            return new ValueAst { value = value };
        }
    }

    public class VariableAst : ExprAst
    {
        public string varName;

        public static VariableAst Create(string varName)
        {
            return new VariableAst { varName = varName };
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
    }

    public class GetIndexerAst : ExprAst
    {
        public ExprAst instanceExpr;
        public ExprAst indexExpr;

        public static GetIndexerAst Create(ExprAst expr, ExprAst indexExpr)
        {
            return new GetIndexerAst { instanceExpr = expr, indexExpr = indexExpr };
        }
    }

    public abstract class StatementAst
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
    }

    public class FuncCallAst : ExprAst
    {
        public string funcName;
        public ExprAst[] args;
    }

    public class ReturnAst : StatementAst
    {
        public ExprAst expr;

        public static ReturnAst Create(ExprAst expr)
        {
            return new ReturnAst { expr = expr };
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
    }

    public class ReAssignmentVariableAst : StatementAst
    {
        public ExprAst expr;
        public string varName;

        public static ReAssignmentVariableAst Create(string varName, ExprAst expr)
        {
            return new ReAssignmentVariableAst { varName = varName, expr = expr };
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
    }

    public class WhileAst : StatementAst
    {
        public ExprAst conditionExpr;
        public StatementAst[] statements;

        public static WhileAst Create(ExprAst conditionExpr, StatementAst[] statements)
        {
            return new WhileAst { conditionExpr = conditionExpr, statements = statements };
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
    }
}
