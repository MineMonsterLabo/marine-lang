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

        public ValueAst<V> GetValueAst<V>()
        {
            return this as ValueAst<V>;
        }
    }

    public class ValueAst<V> : ExprAst
    {
        public V value;

        public static ValueAst<V> Create(V value)
        {
            return new ValueAst<V> { value = value };
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
    }

    public class FuncCallAst : ExprAst
    {
        public string funcName;
    }

    public class ReturnAst : StatementAst
    {
        public ExprAst expr;

        public static ReturnAst Create(ExprAst expr)
        {
            return new ReturnAst { expr = expr };
        }
    }
}
