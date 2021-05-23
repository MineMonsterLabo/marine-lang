using MarineLang.LexicalAnalysis;
using MarineLang.Models;
using MarineLang.Models.Asts;
using MarineLang.Models.Errors;
using MarineLang.SyntaxAnalysis;
using MineUtil;
using Xunit;

namespace MarineLangUnitTest
{
    public class AstPositionTest
    {
        public IResult<ProgramAst, ParseErrorInfo> ParseHelper(string str)
        {
            var lexer = new LexicalAnalyzer();
            var parser = new SyntaxAnalyzer();

            return parser.Parse(lexer.GetTokens(str));
        }

        [Theory]
        [InlineData(
            @"fun main() 
end", 1, 1, 2, 4)]
        [InlineData(
            @"
fun main()
  ret 1+8
 end", 2, 1, 4, 5)]
        [InlineData(
            @"
fun main()
  ret 1+8
 end
fun fuga()
  ret 666
end", 2, 1, 7, 4)]

        public void ProgramAst(string str, int startLine, int startColumn, int endLine, int endColumn)
        {
            var result = ParseHelper(str);

            Assert.False(result.IsError);
            Assert.NotNull(result.Unwrap());
            Assert.Equal(new Position(startLine, startColumn), result.Unwrap().Range.Start);
            Assert.Equal(new Position(endLine, endColumn), result.Unwrap().Range.End);
        }
    }
}
