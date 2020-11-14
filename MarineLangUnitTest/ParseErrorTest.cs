using MarineLang.LexicalAnalysis;
using MarineLang.Models;
using MarineLang.Models.Asts;
using MarineLang.Models.Errors;
using MarineLang.Streams;
using MarineLang.SyntaxAnalysis;
using System.Linq;
using Xunit;

namespace MarineLangUnitTest
{
    public class ParseErrorTest
    {
        public IParseResult<ProgramAst> ParseHelper(string str)
        {
            var lexer = new LexicalAnalyzer();
            var parser = new SyntaxAnalyzer();

            var tokenStream = TokenStream.Create(lexer.GetTokens(str).ToArray());
            return parser.Parse(tokenStream);
        }

        internal void ErrorCheckHelper(string str, int line, int column, ErrorCode expectedErrorCode)
        {
            var result = ParseHelper(str);

            Assert.True(result.IsError);
            Assert.Equal(expectedErrorCode, result.Error.ErrorCode);
            Assert.Equal(new Position(line, column), result.Error.ErrorPosition);
        }

        [Theory]
        [InlineData("func", 1, 1)]
        [InlineData(" func", 1, 2)]
        [InlineData(" fu fuga() ret 3 end", 1, 2)]
        [InlineData(
@"
func", 2, 1)]
        public void ErrorNonFuncWord(string str, int line, int column)
        {
            ErrorCheckHelper(str, line, column, ErrorCode.SyntaxNonFuncWord);
        }

        [Theory]
        [InlineData("fun", 1, 4)]
        [InlineData(" fun", 1, 5)]
        [InlineData(" fun ret 3 end", 1, 5)]
        [InlineData(" fun let a=5 end", 1, 5)]
        [InlineData(
@"fun ()
ret 4 end", 1, 4)]
        public void ErrorNonFuncName(string str, int line, int column)
        {
            ErrorCheckHelper(str, line, column, ErrorCode.SyntaxNonFuncName);
        }

        [Theory]
        [InlineData("fun hoge", 1, 9)]
        [InlineData("fun hoge fuga() end", 1, 9)]
        [InlineData(" fun hoge fuga() end", 1, 10)]
        [InlineData(" fun hoge(ret) end", 1, 10)]
        [InlineData(" fun hoge(111) end", 1, 10)]
        [InlineData(
@"fun 
hoge(111) end", 2, 5)]
        public void ErrorNonFuncParen(string str, int line, int column)
        {
            ErrorCheckHelper(str, line, column, ErrorCode.SyntaxNonFuncParen);
        }

        [Theory]
        [InlineData("fun hoge()", 1, 11)]
        [InlineData("fun hoge() ret 77", 1, 18)]
        [InlineData("fun hoge() let a=3", 1, 19)]
        public void ErrorNonEndWord(string str, int line, int column)
        {
            ErrorCheckHelper(str, line, column, ErrorCode.SyntaxNonEndWord);
        }

        [Theory]
        [InlineData("fun hoge() ret end", 1, 16)]
        [InlineData("fun hoge() ret ret 5", 1, 16)]
        [InlineData("fun hoge() ret let a=3", 1, 16)]
        [InlineData("fun hoge() ret", 1, 15)]
        public void ErrorNonRetExpr(string str, int line, int column)
        {
            ErrorCheckHelper(str, line, column, ErrorCode.SyntaxNonRetExpr);
        }

        [Theory]
        [InlineData("fun hoge() let a=", 1, 17)]
        [InlineData("fun hoge() a=", 1, 13)]
        [InlineData("fun hoge() a= end", 1, 15)]
        [InlineData("fun hoge() let a= end", 1, 19)]
        public void ErrorNonEqualExpr(string str, int line, int column)
        {
            ErrorCheckHelper(str, line, column, ErrorCode.SyntaxNonEqualExpr);
        }

        [Theory]
        [InlineData("fun hoge() let", 1, 15)]
        [InlineData("fun hoge() let 66", 1, 16)]
        [InlineData("fun hoge() let ret", 1, 16)]
        [InlineData("fun hoge() let 44 end", 1, 16)]
        [InlineData("fun hoge() let end", 1, 16)]
        public void ErrorNonLetVarName(string str, int line, int column)
        {
            ErrorCheckHelper(str, line, column, ErrorCode.SyntaxNonLetVarName);
        }

        [Theory]
        [InlineData("fun hoge() let a", 1, 17)]
        [InlineData("fun hoge() let re", 1, 18)]
        [InlineData("fun hoge() let bbb end", 1, 20)]
        [InlineData("fun hoge() let cd end", 1, 19)]
        public void ErrorNonLetEqual(string str, int line, int column)
        {
            ErrorCheckHelper(str, line, column, ErrorCode.SyntaxNonLetEqual);
        }

    }
}
