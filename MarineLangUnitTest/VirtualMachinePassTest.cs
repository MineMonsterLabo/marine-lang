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
            var parser = new Parser();

            var tokenStream = TokenStream.Create(lexer.GetTokens(str).ToArray());
            var parseResult = parser.Parse(tokenStream);
            if (parseResult.IsError)
                return null;
            var vm = new VirtualMachine();

            vm.SetProgram(parseResult.Value);
            return vm;
        }

        public static void hello() { }
        public static int ret_123() { return 123; }


        [Theory]
        [InlineData("fun main() hello() end")]
        [InlineData(@"
fun main() foo_bar() end 
fun foo_bar() hello() end"
        )]
        public void CallMarineLangFunc(string str)
        {
            var vm = VmCreateHelper(str);

            Assert.NotNull(vm);

            vm.Register(typeof(VirtualMachinePassTest).GetMethod("hello"));

            vm.Run<UnitType>("main");
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
            var vm = VmCreateHelper(str);

            Assert.NotNull(vm);

            vm.Register(typeof(VirtualMachinePassTest).GetMethod("ret_123"));
            vm.Register(typeof(VirtualMachinePassTest).GetMethod("hello"));

            var ret = vm.Run<int>("main");

            Assert.Equal(123, ret);
        }

        [Theory]
        [InlineData("fun main() ret false end", false)]
        [InlineData("fun main() ret true end", true)]
        public void CallMarineLangFuncRetBool(string str, bool flag)
        {
            var vm = VmCreateHelper(str);

            Assert.NotNull(vm);

            var ret = vm.Run<bool>("main");

            Assert.Equal(flag, ret);
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
            var vm = VmCreateHelper(str);

            Assert.NotNull(vm);

            var ret = vm.Run<char>("main");

            Assert.Equal(c, ret);
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
            var vm = VmCreateHelper(str);

            Assert.NotNull(vm);

            var ret = vm.Run<string>("main");

            Assert.Equal(expected, ret);
        }
    }
}
