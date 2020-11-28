using System.Collections.Generic;
using System.Linq;

namespace MarineLang.Models.Asts
{

    public interface IAst
    {
        IEnumerable<T> LookUp<T>();
        Position Start { get; }
        Position End { get; }
    }

    public class ProgramAst : IAst
    {
        public FuncDefinitionAst[] funcDefinitionAsts;

        public Position Start => funcDefinitionAsts.Length == 0 ? new Position() : funcDefinitionAsts[0].Start;
        public Position End => funcDefinitionAsts.Length == 0 ? new Position() : funcDefinitionAsts.Last().End;

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
        Token funToken;
        public string funcName;
        public VariableAst[] args;
        public StatementAst[] statementAsts;
        Token endToken;

        public Position Start => funToken.position;
        public Position End => endToken.PositionEnd;

        public static FuncDefinitionAst Create(Token funToken, string funcName, VariableAst[] args, StatementAst[] statementAsts, Token endToken)
        {
            return new FuncDefinitionAst
            {
                funToken = funToken,
                funcName = funcName,
                args = args,
                statementAsts = statementAsts,
                endToken = endToken
            };
        }

        public IEnumerable<T> LookUp<T>()
        {
            return
                args.SelectMany(x => x.LookUp<T>())
                .Concat(statementAsts.SelectMany(x => x.LookUp<T>()));
        }
    }
}
