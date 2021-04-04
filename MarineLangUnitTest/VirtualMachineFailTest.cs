using MarineLang.BuiltInTypes;
using MarineLang.Models;
using MarineLang.Models.Errors;
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

            Assert.Equal(expected, ret.Value);
        }

        [Theory]
        [InlineData("fun main() let hoge = create_hoge() hoge.test() end", 1, 42)]
        [InlineData("fun main() let hoge = create_hoge() hoge.test end", 1, 42)]
        [InlineData("fun main() let hoge = create_hoge() hoge.test = 20 end", 1, 42)]
        [InlineData("fun main() let hoge = create_hoge() hoge.test[0] end", 1, 42)]
        [InlineData("fun main() let hoge = create_hoge() hoge.test[0] = 10 end", 1, 42)]
        public void MemberNotFoundThrowTest(string str, int line = 0, int column = 0)
        {
            var exception = Assert.Throws<MarineRuntimeException>(() => RunReturnCheck(str, new UnitType()));
            Assert.Equal(ErrorCode.RuntimeMemberNotFound, exception.RuntimeErrorInfo.ErrorCode);
            Assert.Equal(new Position(line, column), exception.RuntimeErrorInfo.errorPosition);
        }

        [Theory]
        [InlineData("fun main() let hoge = create_hoge() hoge.name[0] end", 1, 46)]
        [InlineData("fun main() let hoge = create_hoge() hoge.name[0] = 20 end", 1, 46)]
        [InlineData("fun main() let hoge = create_hoge() hoge.names['0'] end", 1, 47)]
        [InlineData("fun main() let hoge = create_hoge() hoge.names['0'] = 10 end", 1, 47)]
        public void IndexerNotFoundThrowTest(string str, int line = 0, int column = 0)
        {
            var exception = Assert.Throws<MarineRuntimeException>(() => RunReturnCheck(str, new UnitType()));
            Assert.Equal(ErrorCode.RuntimeIndexerNotFound, exception.RuntimeErrorInfo.ErrorCode);
            Assert.Equal(new Position(line, column), exception.RuntimeErrorInfo.errorPosition);
        }

        [Theory]
        [InlineData("fun main() let piyo = create_piyo() ret piyo.member1 end ", 12, 1, 46)]
        [InlineData("fun main() let piyo = create_piyo() piyo.member1 = 20 ret piyo.member1 end ", 20, 1, 42)]
        [InlineData("fun main() let piyo = create_piyo() ret piyo.member2 end ", "hello", 1, 46)]
        [InlineData("fun main() let piyo = create_piyo() piyo.member2 = \"hello2\" ret piyo.member2 end ", "hello2", 1,
            42)]
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