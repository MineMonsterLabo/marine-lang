using MarineLang;
using System.Linq;
using Xunit;

namespace MarineLangUnitTest
{
    public class AstPassTest
    {
        public ParseResult<ProgramAst> ParseHelper(string str)
        {
            var lexer = new Lexer();
            var parser = new Parser();

            var tokenStream = TokenStream.Create(lexer.GetTokens(str).ToArray());
            return parser.Parse(tokenStream);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("  ")]
        [InlineData("\t")]
        [InlineData("\r\n")]
        [InlineData("\r")]
        [InlineData("\n")]
        [InlineData("\n \r\n  \r  ")]
        public void EmptyProgram(string str)
        {
            var result = ParseHelper(str);

            Assert.False(result.isError);
            Assert.NotNull(result.ast);
            Assert.Empty(result.ast.funcDefinitionAsts);
        }

        [Theory]
        [InlineData("fun hoge_fuga()  end")]
        [InlineData("fun hoge_fuga()  end  ")]
        [InlineData("fun hoge_fuga()end")]
        [InlineData("  fun hoge_fuga  ()     end")]
        [InlineData("\n  fun hoge_fuga \r\n () \n  \r  end \n")]
        public void EmptyFunc(string str)
        {
            var result = ParseHelper(str);

            Assert.False(result.isError);
            Assert.NotNull(result.ast);
            Assert.Single(result.ast.funcDefinitionAsts);
            var funcDefinitionAst = result.ast.funcDefinitionAsts[0];
            Assert.Equal("hoge_fuga", funcDefinitionAst.funcName);
            Assert.Empty(funcDefinitionAst.statementAsts);
        }

        [Theory]
        [InlineData("fun hoge_fuga() f() end")]
        [InlineData("fun hoge_fuga() f() end  ")]
        [InlineData("fun hoge_fuga()f()end")]
        [InlineData("  fun hoge_fuga  ()  f ( )   end")]
        [InlineData("\n  fun hoge_fuga \r\n () \n f ( ) \r  end \n")]
        public void OneCallFunc(string str)
        {
            var result = ParseHelper(str);

            Assert.False(result.isError);
            Assert.NotNull(result.ast);
            Assert.Single(result.ast.funcDefinitionAsts);
            var funcDefinitionAst = result.ast.funcDefinitionAsts[0];
            Assert.Equal("hoge_fuga", funcDefinitionAst.funcName);
            Assert.Single(funcDefinitionAst.statementAsts);
            Assert.Equal("f", funcDefinitionAst.statementAsts[0].funcName);
        }
    }
}
