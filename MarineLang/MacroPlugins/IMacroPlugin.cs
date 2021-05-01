using MarineLang.Models;
using MarineLang.Models.Asts;
using MarineLang.SyntaxAnalysis;
using System.Collections.Generic;

namespace MarineLang.MacroPlugins
{
    public interface IMacroPlugin<T>
    {
        IParseResult<T> Replace(MarineParser marineParser, List<Token> tokens);
    }

    public interface IFuncDefinitionMacroPlugin : IMacroPlugin<IEnumerable<FuncDefinitionAst>> { }
}
