using System.Runtime.CompilerServices;

namespace MarineLang
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
        public FuncCallAst[] statementAsts;

        public static FuncDefinitionAst Create(string funcName, FuncCallAst[] statementAsts)
        {
            return new FuncDefinitionAst { funcName = funcName, statementAsts = statementAsts };
        }
    }

    public class FuncCallAst
    {
        public string funcName;
    }
}
