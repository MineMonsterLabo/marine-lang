using MarineLang;
using MarineLang.BuiltInTypes;
using MarineLang.LexicalAnalysis;
using MarineLang.Streams;
using MarineLang.SyntaxAnalysis;
using System.Linq;
using Xunit;

namespace MarineLangUnitTest
{
    public class VirtualMachinePassTest
    {

        public VirtualMachine VmCreateHelper(string str)
        {
            var lexer = new Lexer();
            var parser = new SyntaxAnalyzer();

            var tokens = lexer.GetTokens(str).ToArray();
            var tokenStream = TokenStream.Create(tokens);
            var parseResult = parser.Parse(tokenStream);
            if (parseResult.IsError)
                return null;
            var vm = new VirtualMachine();

            vm.SetProgram(parseResult.Value);
            vm.Register(typeof(VirtualMachinePassTest).GetMethod("ret_123"));
            vm.Register(typeof(VirtualMachinePassTest).GetMethod("hello"));
            vm.Register(typeof(VirtualMachinePassTest).GetMethod("plus"));
            vm.Register(typeof(VirtualMachinePassTest).GetMethod("two"));
            vm.Register(typeof(VirtualMachinePassTest).GetMethod("not"));

            return vm;
        }

        internal void RunReturnCheck<RET>(string str, RET expected)
        {
            var vm = VmCreateHelper(str);

            Assert.NotNull(vm);

            var ret = vm.Run<RET>("main");

            Assert.Equal(expected, ret);
        }

        public static void hello() { }
        public static int ret_123() { return 123; }
        public static int plus(int a, int b) { return a + b; }
        public static int two(int a) { return a * 2; }
        public static bool not(bool a) { return !a; }

        [Theory]
        [InlineData("fun main() hello() end")]
        [InlineData(@"
fun main() foo_bar() end 
fun foo_bar() hello() end"
        )]
        public void CallMarineLangFunc(string str)
        {
            RunReturnCheck(str, new UnitType());
        }

        [Theory]
        [InlineData("fun main() ret 123 end")]
        [InlineData("fun main() ret 123 ret 115 end")]
        [InlineData("fun main() ret 123 hogehoge() end")]
        [InlineData("fun main() hello() ret 123 end")]
        [InlineData("fun main() ret ret_123() end")]
        [InlineData(@"
fun main() ret fuga() end
fun fuga() ret 123 end
")]
        public void CallMarineLangFuncRetInt(string str)
        {
            RunReturnCheck(str, 123);
        }

        [Theory]
        [InlineData("fun main() ret false end", false)]
        [InlineData("fun main() ret true end", true)]
        public void CallMarineLangFuncRetBool(string str, bool flag)
        {
            RunReturnCheck(str, flag);
        }

        [Theory]
        [InlineData("fun main() ret 'c' end", 'c')]
        [InlineData("fun main() ret '\\'' end", '\'')]
        [InlineData("fun main() ret '\\\\' end", '\\')]
        [InlineData("fun main() ret '\\n' end", '\n')]
        [InlineData("fun main() ret '\\t' end", '\t')]
        [InlineData("fun main() ret '\\r' end", '\r')]
        [InlineData("fun main() ret 'あ' end", 'あ')]
        public void CallMarineLangFuncRetChar(string str, char c)
        {
            RunReturnCheck(str, c);
        }

        [Theory]
        [InlineData("fun main() ret \"hoge\" end", "hoge")]
        [InlineData("fun main() ret \"\\\"\" end", "\"")]
        [InlineData("fun main() ret \"\\\\\" end", "\\")]
        [InlineData("fun main() ret \"\\n\" end", "\n")]
        [InlineData("fun main() ret \"\\t\" end", "\t")]
        [InlineData("fun main() ret \"\\r\" end", "\r")]
        [InlineData("fun main() ret \"あ\" end", "あ")]
        [InlineData("fun main() ret \"あ\\rhoge\\\"\" end", "あ\rhoge\"")]
        public void CallMarineLangFuncRetString(string str, string expected)
        {
            RunReturnCheck(str, expected);

        }

        [Theory]
        [InlineData("fun main() ret 1.2 end", 1.2f)]
        [InlineData("fun main() ret 5.0 end", 5.0f)]
        [InlineData("fun main() ret 3.3333214 end", 3.3333214f)]
        public void CallMarineLangFuncRetFloat(string str, float expected)
        {
            RunReturnCheck(str, expected);
        }

        [Theory]
        [InlineData("fun main() ret plus(3,2) end", 5)]
        [InlineData("fun main() ret two(3) end", 6)]
        [InlineData("fun main() ret plus(1,two(two(3))) end", 13)]
        public void CallCsharpFuncWithArgs(string str, int expected)
        {
            RunReturnCheck(str, expected);
        }

        [Theory]
        [InlineData("fun main() ret not(not(false)) end", false)]
        [InlineData("fun main() ret not(not(true)) end", true)]
        public void CallCsharpFuncWithArgs2(string str, bool expected)
        {
            RunReturnCheck(str, expected);
        }

        [Theory]
        [InlineData("fun main() let abc_d=false ret abc_d end", false)]
        [InlineData("fun main() let hoge=5 hoge=3 ret hoge end", 3)]
        [InlineData("fun main() let left=5 let right=3 ret plus(left,right) end", 8)]
        [InlineData("fun main() let str=\"あいうえお\"ret str end", "あいうえお")]
        [InlineData("fun main() let ccc = '$'let aa=\"abab\" ret ccc end", '$')]
        [InlineData(@"
fun main() let a = 13 ret plus(f(),a) end
fun f() let a=3 ret a end
", 16)]
        public void Variable<T>(string str, T expected)
        {
            RunReturnCheck(str, expected);
        }

        [Theory]
        [InlineData("fun main() ret id(4) end fun id(a) ret a end", 4)]
        [InlineData("fun main() ret f(\"abc\",'c',4,3.5) end fun f(a,b,c,d) ret a end", "abc")]
        [InlineData("fun main() ret f(\"abc\",'c',4,3.5) end fun f(a,b,c,d) ret b end", 'c')]
        [InlineData("fun main() ret f(\"abc\",'c',4,3.5) end fun f(a,b,c,d) ret c end", 4)]
        [InlineData("fun main() ret f(\"abc\",'c',4,3.5) end fun f(a,b,c,d) ret d end", 3.5f)]
        public void CallMarineFuncWithArgs<T>(string str, T expected)
        {
            RunReturnCheck(str, expected);
        }

        [Theory]
        [InlineData("fun main(a,b) ret plus(a,b) end ")]
        public void CallMarineFuncWithArgs2(string str)
        {
            var vm = VmCreateHelper(str);

            Assert.NotNull(vm);

            var ret = vm.Run<int>("main", 12, 8);

            Assert.Equal(20, ret);
        }
    }
}
