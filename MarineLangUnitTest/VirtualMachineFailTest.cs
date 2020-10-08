using System;
using System.Linq;
using MarineLang.LexicalAnalysis;
using MarineLang.Streams;
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
        [InlineData("fun main() let piyo = create_piyo() ret piyo.member1 end ", 12)]
        [InlineData("fun main() let piyo = create_piyo() piyo.member1 = 20 ret piyo.member1 end ", 20)]
        [InlineData("fun main() let piyo = create_piyo() ret piyo.member2 end ", "hello")]
        [InlineData("fun main() let piyo = create_piyo() piyo.member2 = \"hello2\" ret piyo.member2 end ", "hello2")]
        [InlineData("fun main() let piyo = create_piyo() ret piyo.member3[0] end ", "hello")]
        [InlineData("fun main() let piyo = create_piyo() piyo.member3[0] = \"hello2\" ret piyo.member3[0] end ",
            "hello2")]
        [InlineData("fun main() let piyo = create_piyo() ret piyo.plus(2, 5) end ", 7)]
        public void AccessibilityThrowTest<T>(string str, T expected)
        {
            Assert.Throws<MemberAccessException>(() => RunReturnCheck(str, expected));
        }
    }
}