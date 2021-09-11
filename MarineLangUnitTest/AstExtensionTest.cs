using MarineLang.LexicalAnalysis;
using MarineLang.Models.Asts;
using MarineLang.SyntaxAnalysis;
using System.Linq;
using Xunit;

namespace MarineLangUnitTest
{
    public class AstExtensionTest
    {
        [Fact]
        public void LookUpTest()
        {
            var source = @"
fun main(a,b)
    let cc = 5+2
    let ccc = sub()
end

fun sub()
    ret 4
end
";
            var tokens = new LexicalAnalyzer().GetTokens(source);
            var ast = new SyntaxAnalyzer().Parse(tokens).programAst;

            Assert.Single(ast.LookUp<ProgramAst>());
            Assert.Equal(2, ast.LookUp<FuncDefinitionAst>().Count());
            Assert.Equal(2, ast.LookUp<AssignmentVariableAst>().Count());
            Assert.Equal(4, ast.LookUp<VariableAst>().Count());
            Assert.Equal(3, ast.LookUp<ValueAst>().Count());
            Assert.Single(ast.LookUp<ReturnAst>());
            Assert.Single(ast.LookUp<BinaryOpAst>());
            Assert.Single(ast.LookUp<FuncCallAst>());
        }
    }
}
