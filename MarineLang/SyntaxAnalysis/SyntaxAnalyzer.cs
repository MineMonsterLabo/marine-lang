using MarineLang.MacroPlugins;
using MarineLang.Models;
using MarineLang.Models.Asts;
using MarineLang.Models.Errors;
using MarineLang.Inputs;
using System.Collections.Generic;
using System.Linq;

namespace MarineLang.SyntaxAnalysis
{
    public class SyntaxAnalyzer
    {
        public readonly SyntaxParser marineParser;

        public SyntaxAnalyzer(PluginContainer pluginContainer = null)
        {
            if (pluginContainer == null)
                pluginContainer = new PluginContainer();

            marineParser = new SyntaxParser(pluginContainer);
        }

        public SyntaxParseResult Parse(IEnumerable<Token> tokens)
        {
            var input = TokenInput.Create(tokens.ToArray());
            var result = marineParser.ParseProgram(input);
            return new SyntaxParseResult(result.Result.RawValue,result.ErrorStack);
        }
    }

    public class SyntaxParseResult
    {
        public readonly ProgramAst programAst;
        public readonly IEnumerable<ParseErrorInfo> parseErrorInfos;

        public bool IsError => parseErrorInfos.Any();
        public bool IsOk => !IsError;

        public SyntaxParseResult(ProgramAst programAst, IEnumerable<ParseErrorInfo> parseErrorInfos)
        {
            this.programAst = programAst;
            this.parseErrorInfos = parseErrorInfos;
        }
    }
}
