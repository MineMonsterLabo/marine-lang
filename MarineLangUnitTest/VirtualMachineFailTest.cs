using System.Linq;
using MarineLang.LexicalAnalysis;
using MarineLang.Models;
using MarineLang.Models.Errors;
using MarineLang.SyntaxAnalysis;
using MarineLang.VirtualMachines;
using MarineLang.VirtualMachines.Attributes;
using MarineLangUnitTest.Helper;
using Xunit;

namespace MarineLangUnitTest
{
    public class VirtualMachineFailTest
    {
        internal void RunReturnCheck<RET>(string str, RET expected)
        {
            var vm = VmCreateHelper.Create(str);

            Assert.NotNull(vm);

            var ret = vm.Run<RET>("main");

            Assert.Equal(expected, ret);
        }

        [Theory]
        [InlineData("fun main() let piyo = create_piyo() ret piyo.member1 end ", 12, 1, 46)]
        [InlineData("fun main() let piyo = create_piyo() piyo.member1 = 20 ret piyo.member1 end ", 20, 1, 42)]
        [InlineData("fun main() let piyo = create_piyo() ret piyo.member2 end ", "hello", 1, 46)]
        [InlineData("fun main() let piyo = create_piyo() piyo.member2 = \"hello2\" ret piyo.member2 end ", "hello2", 1, 42)]
        [InlineData("fun main() let piyo = create_piyo() ret piyo.member3[0] end ", "hello", 1, 46)]
        [InlineData("fun main() let piyo = create_piyo() piyo.member3[0] = \"hello2\" ret piyo.member3[0] end ",
            "hello2", 1, 42)]
        [InlineData("fun main() let piyo = create_piyo() ret piyo.plus(2, 5) end ", 7, 1, 46)]
        public void AccessibilityThrowTest<T>(string str, T expected, int line = 0, int column = 0)
        {
            var exception = Assert.Throws<MarineRuntimeException>(() => RunReturnCheck(str, expected));
            Assert.Equal(ErrorCode.RuntimeMemberAccessPrivate, exception.RuntimeErrorInfo.ErrorCode);
            Assert.Equal(new Position(line, column), exception.RuntimeErrorInfo.errorPosition);
        }
    }
}