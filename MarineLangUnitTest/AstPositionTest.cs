using MarineLang.LexicalAnalysis;
using MarineLang.Models;
using MarineLang.SyntaxAnalysis;
using Xunit;

namespace MarineLangUnitTest
{
    public class AstPositionTest
    {
        public SyntaxParseResult ParseHelper(string str)
        {
            var lexer = new LexicalAnalyzer();
            var parser = new SyntaxAnalyzer();

            return parser.Parse(lexer.GetTokens(str));
        }

        [Theory]
        [InlineData(
            @"fun main() 
end", 0, 1, 1, 15, 2, 4)]
        [InlineData(
            @"
fun main()
  ret 1+8
 end", 2, 2, 1, 28, 4, 5)]
        [InlineData(
            @"
fun main()
  ret 1+8
 end
fun fuga()
  ret 666
end", 2, 2, 1, 56, 7, 4)]

        public void ProgramAst(string str, int startIndex, int startLine, int startColumn, int endIndex, int endLine, int endColumn)
        {
            var result = ParseHelper(str);

            Assert.False(result.IsError);
            Assert.NotNull(result.programAst);
            Assert.Equal(new Position(startIndex, startLine, startColumn), result.programAst.Range.Start);
            Assert.Equal(new Position(endIndex, endLine, endColumn), result.programAst.Range.End);
        }
    }
}
