using System.Collections.Generic;
using System.Linq;

namespace MarineLang.Models.Asts
{

    public interface IAst
    {
        IEnumerable<T> Accept<T>(AstVisitor<T> astVisitor);
        IEnumerable<IAst> GetChildrenWithThis();
        RangePosition Range { get; }
    }

    public class ProgramAst : IAst
    {
        public FuncDefinitionAst[] funcDefinitionAsts;

        public RangePosition Range =>
            funcDefinitionAsts.Length == 0 ?
                new RangePosition() :
                new RangePosition(funcDefinitionAsts[0].Range.Start, funcDefinitionAsts.Last().Range.End);

        public static ProgramAst Create(FuncDefinitionAst[] funcDefinitionAsts)
        {
            return new ProgramAst { funcDefinitionAsts = funcDefinitionAsts };
        }

        public IEnumerable<T>  Accept<T>(AstVisitor<T> astVisitor)
        {
            return Enumerable.Repeat(astVisitor.Visit(this), 1);
        }

        public IEnumerable<IAst> GetChildrenWithThis()
        {
            return 
                Enumerable.Empty<IAst>().Append(this)
                .Concat(funcDefinitionAsts.SelectMany(x => x.GetChildrenWithThis()));
        }
    }

    public class FuncDefinitionAst : IAst
    {
        Token funToken;
        public string funcName;
        public VariableAst[] args;
        public StatementAst[] statementAsts;
        Token endToken;

        public RangePosition Range => new RangePosition(funToken.position, endToken.PositionEnd);

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

        public static FuncDefinitionAst Create(string funcName, VariableAst[] args, StatementAst[] statementAsts)
        {
            return new FuncDefinitionAst
            {
                funcName = funcName,
                args = args,
                statementAsts = statementAsts,
            };
        }

        public IEnumerable<T> Accept<T>(AstVisitor<T> astVisitor)
        {
            return Enumerable.Repeat(astVisitor.Visit(this), 1);
        }

        public IEnumerable<IAst> GetChildrenWithThis()
        {
            return
                Enumerable.Empty<IAst>().Append(this)
                .Concat(args.SelectMany(x => x.GetChildrenWithThis()))
                .Concat(statementAsts.SelectMany(x => x.GetChildrenWithThis()));
        }
    }
}
