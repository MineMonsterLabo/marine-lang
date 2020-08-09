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
            if (parseResult.isError)
                return null;
            var vm = new VirtualMachine();

            vm.SetProgram(parseResult.value);
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
        public void CallMarineLangFuncRet(string str)
        {
            var vm = VmCreateHelper(str);

            Assert.NotNull(vm);

            vm.Register(typeof(VirtualMachinePassTest).GetMethod("ret_123"));
            vm.Register(typeof(VirtualMachinePassTest).GetMethod("hello"));

            var ret = vm.Run<int>("main");

            Assert.Equal(123, ret);
        }
    }
}
