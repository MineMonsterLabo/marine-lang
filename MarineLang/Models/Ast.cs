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

    public abstract class StatementAst
    {
        public FuncCallAst GetFuncCallAst()
        {
            return this as FuncCallAst;
        }

        public ReturnAst GetReturnAst()
        {
            return this as ReturnAst;
        }
    }

    public class FuncCallAst : StatementAst
    {
        public string funcName;
    }

    public class ReturnAst : StatementAst
    {
        public int value;
    }
}
