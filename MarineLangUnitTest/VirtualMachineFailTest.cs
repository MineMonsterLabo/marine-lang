using MarineLang.Models;
using MarineLang.Models.Errors;
using MarineLangUnitTest.Helper;
using MineUtil;
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

            Assert.Equal(expected, ret.Value);
        }

        [Theory]
        [InlineData("fun main() let hoge = create_hoge() hoge.test() end", 36, 1, 37)]
        [InlineData("fun main() let hoge = create_hoge() hoge.test end", 36, 1, 37)]
        [InlineData("fun main() let hoge = create_hoge() hoge.test = 20 end", 36, 1, 37)]
        [InlineData("fun main() let hoge = create_hoge() hoge.test[0] end", 36, 1, 37)]
        [InlineData("fun main() let hoge = create_hoge() hoge.test[0] = 10 end", 36, 1, 37)]
        public void MemberNotFoundThrowTest(string str, int index, int line, int column)
        {
            var exception = Assert.Throws<MarineRuntimeException>(() => RunReturnCheck(str, Unit.Value));
            Assert.Equal(ErrorCode.RuntimeMemberNotFound, exception.RuntimeErrorInfo.ILRuntimeErrorInfo.ErrorCode);
            Assert.Equal(new Position(index, line, column), exception.RuntimeErrorInfo.DebugContexts[0].RangePosition.Start);
        }

        [Theory]
        [InlineData("fun main() let hoge = create_hoge() hoge.Name[0] end", 36, 1, 37)]
        [InlineData("fun main() let hoge = create_hoge() hoge.Name[0] = 20 end", 36, 1, 37)]
        [InlineData("fun main() let hoge = create_hoge() hoge.Names['0'] end", 36, 1, 37)]
        [InlineData("fun main() let hoge = create_hoge() hoge.Names['0'] = 10 end", 36, 1, 37)]
        public void IndexerNotFoundThrowTest(string str, int index, int line, int column)
        {
            var exception = Assert.Throws<MarineRuntimeException>(() => RunReturnCheck(str, Unit.Value));
            Assert.Equal(ErrorCode.RuntimeIndexerNotFound, exception.RuntimeErrorInfo.ILRuntimeErrorInfo.ErrorCode);
            Assert.Equal(new Position(index, line, column), exception.RuntimeErrorInfo.DebugContexts[0].RangePosition.Start);
        }

        [Theory]
        [InlineData("fun main() let piyo = create_piyo() ret piyo.member1 end ", 12, 36, 1, 37)]
        [InlineData("fun main() let piyo = create_piyo() piyo.member1 = 20 ret piyo.member1 end ", 20, 36, 1, 37)]
        [InlineData("fun main() let piyo = create_piyo() ret piyo.Member2 end ", "hello", 36, 1, 37)]
        [InlineData("fun main() let piyo = create_piyo() piyo.Member2 = \"hello2\" ret piyo.Member2 end ", "hello2", 36, 1,
            37)]
        [InlineData("fun main() let piyo = create_piyo() ret piyo.Member3[0] end ", "hello", 36, 1, 37)]
        [InlineData("fun main() let piyo = create_piyo() piyo.Member3[0] = \"hello2\" ret piyo.Member3[0] end ",
            "hello2", 36, 1, 37)]
        [InlineData("fun main() let piyo = create_piyo() ret piyo.plus(2, 5) end ", 7, 36, 1, 37)]
        public void AccessibilityThrowTest<T>(string str, T expected, int index, int line, int column)
        {
            var exception = Assert.Throws<MarineRuntimeException>(() => RunReturnCheck(str, expected));
            Assert.Equal(ErrorCode.RuntimeMemberAccessPrivate, exception.RuntimeErrorInfo.ILRuntimeErrorInfo.ErrorCode);
            Assert.Equal(new Position(index, line, column), exception.RuntimeErrorInfo.DebugContexts[0].RangePosition.Start);
        }

        [Theory]
        [InlineData("fun main() ret (OpSample1.new(30) % 2).v end", 0)]
        public void OpThrowTest<T>(string str, T expected)
        {
            var exception = Assert.Throws<MarineRuntimeException>(() => RunReturnCheck(str, expected));
            Assert.Equal(ErrorCode.RuntimeOperatorNotFound, exception.RuntimeErrorInfo.ILRuntimeErrorInfo.ErrorCode);
        }
    }
}