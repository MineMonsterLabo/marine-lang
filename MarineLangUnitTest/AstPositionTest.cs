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
            Assert.NotNull(result.programAst);
            Assert.Equal(new Position(startLine, startColumn), result.programAst.Range.Start);
            Assert.Equal(new Position(endLine, endColumn), result.programAst.Range.End);
        }
    }
}
