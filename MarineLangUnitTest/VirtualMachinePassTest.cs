using MarineLang;
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

            vm.Run("main");
        }
    }
}
