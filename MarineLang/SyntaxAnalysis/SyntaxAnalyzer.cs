using MarineLang.Models;
using MarineLang.Models.Asts;
using MarineLang.Streams;
using System.Collections.Generic;
using System.Linq;

namespace MarineLang.SyntaxAnalysis
{
    public class SyntaxAnalyzer
    {
        public readonly MarineParser marineParser = new MarineParser();

        public IParseResult<ProgramAst> Parse(IEnumerable<Token> tokens)
        {
            var stream = TokenStream.Create(tokens.ToArray());
            return marineParser.ParseProgram(stream);
        }
    }
}
