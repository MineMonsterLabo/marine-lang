using MarineLang.MacroPlugins;
using MarineLang.Models;
using MarineLang.Models.Asts;
using MarineLang.Streams;
using System.Collections.Generic;
using System.Linq;

namespace MarineLang.SyntaxAnalysis
{
    public class SyntaxAnalyzer
    {
        public readonly MarineParser marineParser;

        public SyntaxAnalyzer(PluginContainer pluginContainer = null)
        {
            if (pluginContainer == null)
                pluginContainer = new PluginContainer();

            marineParser = new MarineParser(pluginContainer);
        }

        public IParseResult<ProgramAst> Parse(IEnumerable<Token> tokens)
        {
            var stream = TokenStream.Create(tokens.ToArray());
            return marineParser.ParseProgram(stream);
        }
    }
}
