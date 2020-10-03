using System.Collections.Generic;
using System.Linq;

namespace MarineLang.Models.Asts
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
}
