using MarineLang.LexicalAnalysis;
using MarineLang.SyntaxAnalysis;
using Xunit;

namespace MarineLangUnitTest
{
    public class AstPassTest
    {
        public SyntaxParseResult ParseHelper(string str)
        {
            var lexer = new LexicalAnalyzer();
            var parser = new SyntaxAnalyzer();

            return parser.Parse(lexer.GetTokens(str));
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

            Assert.False(result.IsError);
            Assert.NotNull(result.programAst);
            Assert.Empty(result.programAst.funcDefinitionAsts);
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

            Assert.False(result.IsError);
            Assert.NotNull(result.programAst);
            Assert.Single(result.programAst.funcDefinitionAsts);
            var funcDefinitionAst = result.programAst.funcDefinitionAsts[0];
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

            Assert.False(result.IsError);
            Assert.NotNull(result.programAst);
            Assert.Single(result.programAst.funcDefinitionAsts);
            var funcDefinitionAst = result.programAst.funcDefinitionAsts[0];
            Assert.Equal("hoge_fuga", funcDefinitionAst.funcName);
            Assert.Single(funcDefinitionAst.statementAsts);
            Assert.Equal(
                "f",
                funcDefinitionAst.statementAsts[0].GetExprStatementAst().expr.GetFuncCallAst().FuncName
            );
        }

        [Theory]
        [InlineData("fun func() ret 111 end")]
        [InlineData("  fun func()ret 111 end")]
        public void RetFunc(string str)
        {
            var result = ParseHelper(str);

            Assert.False(result.IsError);
            Assert.NotNull(result.programAst);
            Assert.Single(result.programAst.funcDefinitionAsts);
            var funcDefinitionAst = result.programAst.funcDefinitionAsts[0];
            Assert.Equal("func", funcDefinitionAst.funcName);
            Assert.Single(funcDefinitionAst.statementAsts);
            Assert.Equal(
                111,
                funcDefinitionAst.statementAsts[0]
                .GetReturnAst().expr
                .GetValueAst().value
            );
        }
    }
}
