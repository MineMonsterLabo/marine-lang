using MarineLang.Models;
using MarineLang.Inputs;
using System.Collections.Generic;
using MarineLang.ParserCore;
using MineUtil;

namespace MarineLang.LexicalAnalysis
{
    public class LexicalAnalyzer
    {
        public IEnumerable<Token> GetTokens(string str)
        {
            IInput<char> input = CharInput.Create(str);
            return LexicalParser.Main()(input).ToResult().Unwrap();
        }
    }
}
