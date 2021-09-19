using MarineLang.LexicalAnalysis;
using MarineLang.Models;
using MarineLang.Models.Errors;
using MarineLang.SyntaxAnalysis;
using System.Linq;
using Xunit;

namespace MarineLangUnitTest
{
    public class ParseErrorTest
    {
        public SyntaxParseResult ParseHelper(string str)
        {
            var lexer = new LexicalAnalyzer();
            var parser = new SyntaxAnalyzer();

            return parser.Parse(lexer.GetTokens(str));
        }

        internal void ErrorCheckHelper(string str, int startLine, int startColumn, int endLine, int endColumn, ErrorCode expectedErrorCode)
        {
            var result = ParseHelper(str);

            Assert.True(result.IsError);
            var error = result.parseErrorInfos.First();
            Assert.Equal(expectedErrorCode, error.ErrorCode);
            Assert.Equal(new RangePosition(new Position(startLine, startColumn), new Position(endLine, endColumn)), error.ErrorRangePosition);
        }

        internal void ErrorCheckHelper(SyntaxParseResult result,int errorIndex, int startLine, int startColumn, int endLine, int endColumn, ErrorCode expectedErrorCode)
        {
            Assert.True(result.IsError);
            var error = result.parseErrorInfos.ElementAt(errorIndex);
            Assert.Equal(expectedErrorCode, error.ErrorCode);
            Assert.Equal(new RangePosition(new Position(startLine, startColumn), new Position(endLine, endColumn)), error.ErrorRangePosition);
        }

        [Theory]
        [InlineData("func", 1, 1,1,5)]
        [InlineData(" func", 1, 2,1,6)]
        [InlineData(" fu fuga() ret 3 end", 1, 2,1,4)]
        [InlineData(
@"
func", 2, 1,2,5)]
        public void ErrorNonFuncWord(string str, int startLine, int startColumn, int endLine, int endColumn)
        {
            ErrorCheckHelper(str, startLine, startColumn, endLine, endColumn, ErrorCode.SyntaxNonFuncWord);
        }

        [Theory]
        [InlineData("fun", 1,1,1, 4)]
        [InlineData(" fun",1,2 ,1, 5)]
        [InlineData(" fun ret 3 end",1,2, 1, 5)]
        [InlineData(" fun let a=5 end", 1,2,1, 5)]
        [InlineData(
@"fun ()
ret 4 end",1,1, 1, 4)]
        public void ErrorNonFuncName(string str, int startLine, int startColumn, int endLine, int endColumn)
        {
            ErrorCheckHelper(str, startLine, startColumn, endLine, endColumn, ErrorCode.SyntaxNonFuncName);
        }

        [Theory]
        [InlineData("fun hoge", 1,5,1, 9)]
        [InlineData("fun hoge fuga() end", 1,5,1, 9)]
        [InlineData(" fun hoge fuga() end", 1, 6, 1, 10)]
        [InlineData(" fun hoge(ret) end", 1, 6, 1, 10)]
        [InlineData(" fun hoge(111) end", 1, 6, 1, 10)]
        [InlineData(
@"fun 
hoge(111) end", 2, 1, 2, 5)]
        public void ErrorNonFuncParen(string str, int startLine, int startColumn, int endLine, int endColumn)
        {
            ErrorCheckHelper(str, startLine, startColumn, endLine, endColumn, ErrorCode.SyntaxNonFuncParen);
        }

        [Theory]
        [InlineData("fun hoge()", 1,10, 1, 11)]
        [InlineData("fun hoge() ret 77", 1, 16, 1, 18)]
        [InlineData("fun hoge() let a=3", 1, 18, 1, 19)]
        public void ErrorNonEndWord(string str, int startLine, int startColumn, int endLine, int endColumn)
        {
            ErrorCheckHelper(str, startLine, startColumn, endLine, endColumn, ErrorCode.SyntaxNonEndWord);
        }

        [Theory]
        [InlineData("fun hoge() ret end", 1, 16, 1, 19)]
        [InlineData("fun hoge() ret ret 5", 1, 16, 1, 19)]
        [InlineData("fun hoge() ret let a=3", 1, 16, 1, 19)]
        [InlineData("fun hoge() ret", 1, 12, 1, 15)]
        public void ErrorNonRetExpr(string str, int startLine, int startColumn, int endLine, int endColumn)
        {
            ErrorCheckHelper(str, startLine, startColumn, endLine, endColumn, ErrorCode.SyntaxNonRetExpr);
        }

        [Theory]
        [InlineData("fun hoge() let a=", 1,17,1, 18)]
        [InlineData("fun hoge() a=", 1,13,1, 14)]
        [InlineData("fun hoge() a= end",1,15, 1, 18)]
        [InlineData("fun hoge() let a= end",1,19, 1, 22)]
        public void ErrorNonEqualExpr(string str, int startLine, int startColumn, int endLine, int endColumn)
        {
            ErrorCheckHelper(str, startLine, startColumn, endLine, endColumn, ErrorCode.SyntaxNonEqualExpr);
        }

        [Theory]
        [InlineData("fun hoge() let",1,12, 1, 15)]
        [InlineData("fun hoge() let 66", 1,16,1, 18)]
        [InlineData("fun hoge() let ret", 1,16,1, 19)]
        [InlineData("fun hoge() let 44 end", 1,16,1, 18)]
        [InlineData("fun hoge() let end",1,16, 1, 19)]
        public void ErrorNonLetVarName(string str, int startLine, int startColumn, int endLine, int endColumn)
        {
            ErrorCheckHelper(str, startLine, startColumn, endLine, endColumn, ErrorCode.SyntaxNonLetVarName);
        }

        [Theory]
        [InlineData("fun hoge() let a",1,16, 1, 17)]
        [InlineData("fun hoge() let re",1,16, 1, 18)]
        [InlineData("fun hoge() let bbb end", 1,20,1, 23)]
        [InlineData("fun hoge() let cd end",1,19, 1, 22)]
        public void ErrorNonLetEqual(string str, int startLine, int startColumn, int endLine, int endColumn)
        {
            ErrorCheckHelper(str, startLine, startColumn, endLine, endColumn, ErrorCode.SyntaxNonLetEqual);
        }

        [Theory]
        [InlineData("fun hoge() ret 77 fun fuga() ret 55")]
        public void MultiErrorTest1(string str)
        {
            var result = ParseHelper(str);
            Assert.Equal(2, result.parseErrorInfos.Count());

            ErrorCheckHelper(result, 0, 1, 19, 1, 22, ErrorCode.SyntaxNonExpectedFuncWord);
            ErrorCheckHelper(result, 1, 1, 34, 1, 36, ErrorCode.SyntaxNonEndWord);
        }

        [Theory]
        [InlineData("fun hoge() let ret")]
        public void MultiErrorTest2(string str)
        {
            var result = ParseHelper(str);
            Assert.Equal(3, result.parseErrorInfos.Count());

            ErrorCheckHelper(result, 0, 1, 16, 1, 19, ErrorCode.SyntaxNonLetVarName);
            ErrorCheckHelper(result, 1, 1, 16, 1, 19, ErrorCode.SyntaxNonRetExpr);
            ErrorCheckHelper(result, 2, 1, 16, 1, 19, ErrorCode.SyntaxNonEndWord);
        }

        [Theory]
        [InlineData("fun hoge() 333. end fun fuga() end")]
        [InlineData("fun hoge() for i = 1 , 10 , 1{ end fun fuga() end")]
        [InlineData("fun hoge() for i = 1 , 10 ,  end fun fuga() end")]
        [InlineData("fun hoge() for end fun fuga() end")]
        [InlineData("fun hoge() foreach val in [1,2,3]{ end fun fuga() end")]
        [InlineData("fun hoge() while i<10{ end fun fuga() end")]
        [InlineData("fun hoge() while i<10 end fun fuga() end")]
        [InlineData("fun hoge() while  end fun fuga() end")]
        [InlineData("fun hoge() if true { end fun fuga() end")]
        [InlineData("fun hoge() if true {1} else{ end fun fuga() end")]
        [InlineData("fun hoge() hoge:: end fun fuga() end")]
        [InlineData("fun hoge() 4+ end fun fuga() end")]
        [InlineData("fun hoge() wait_wait5().await. end fun fuga() end")]
        [InlineData("fun hoge() let fuga = create_fuga() fuga.member1 =  end fun fuga() end")]
        [InlineData("fun hoge() StaticType.member1 =  end fun fuga() end")]
        public void MultiErrorTest3(string str)
        {
            var result = ParseHelper(str);

            Assert.Single(result.parseErrorInfos);
            Assert.Equal(2, result.programAst.funcDefinitionAsts.Length);
            Assert.Equal("hoge", result.programAst.funcDefinitionAsts[0].funcName);
            Assert.Equal("fuga", result.programAst.funcDefinitionAsts[1].funcName);
        }

    }
}
