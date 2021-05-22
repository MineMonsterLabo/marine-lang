using MarineLang.Models;
using MarineLang.Models.Asts;
using MarineLang.Models.Errors;
using MarineLang.SyntaxAnalysis;
using MineUtil;
using System.Collections.Generic;

namespace MarineLang.MacroPlugins
{
    public interface IMacroPlugin<T>
    {
        IResult<T, ParseErrorInfo> Replace(MarineParser marineParser, List<Token> tokens);
    }

    public interface IFuncDefinitionMacroPlugin : IMacroPlugin<IEnumerable<FuncDefinitionAst>> { }
    public interface IExprMacroPlugin : IMacroPlugin<ExprAst> { }
}
