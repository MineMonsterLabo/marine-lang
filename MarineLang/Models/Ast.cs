using System.Runtime.CompilerServices;

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
        public StatementAst[] statementAsts;

        public static FuncDefinitionAst Create(string funcName, StatementAst[] statementAsts)
        {
            return new FuncDefinitionAst { funcName = funcName, statementAsts = statementAsts };
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

        public AssignmentAst GetAssignmentAst()
        {
            return this as AssignmentAst;
        }

        public ReAssignmentAst GetReAssignmentAst()
        {
            return this as ReAssignmentAst;
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

    public class AssignmentAst : StatementAst
    {
        public ExprAst expr;
        public string varName;

        public static AssignmentAst Create(string varName, ExprAst expr)
        {
            return new AssignmentAst { varName = varName, expr = expr };
        }
    }

    public class ReAssignmentAst : StatementAst
    {
        public ExprAst expr;
        public string varName;

        public static ReAssignmentAst Create(string varName, ExprAst expr)
        {
            return new ReAssignmentAst { varName = varName, expr = expr };
        }
    }
}
