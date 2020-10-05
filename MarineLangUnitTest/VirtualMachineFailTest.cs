using System;
using System.Linq;
using MarineLang.LexicalAnalysis;
using MarineLang.Streams;
using MarineLang.SyntaxAnalysis;
using MarineLang.VirtualMachines;
using MarineLang.VirtualMachines.Attributes;
using Xunit;

namespace MarineLangUnitTest
{
    public class VirtualMachineFailTest
    {
        public HighLevelVirtualMachine VmCreateHelper(string str)
        {
            var lexer = new Lexer();
            var parser = new SyntaxAnalyzer();

            var tokens = lexer.GetTokens(str).ToArray();
            var tokenStream = TokenStream.Create(tokens);
            var parseResult = parser.Parse(tokenStream);
            if (parseResult.IsError)
                return null;
            var vm = new HighLevelVirtualMachine();

            vm.SetProgram(parseResult.Value);
            vm.GlobalFuncRegister(typeof(VirtualMachineFailTest).GetMethod(nameof(CreateFuga)));

            vm.Compile();

            return vm;
        }

        internal void RunReturnCheck<RET>(string str, RET expected)
        {
            var vm = VmCreateHelper(str);

            Assert.NotNull(vm);

            var ret = vm.Run<RET>("main");

            Assert.Equal(expected, ret);
        }

        public class Fuga
        {
            [MemberPrivate] public int member1 = 12;
            [MemberPrivate] public string Member2 { get; set; } = "hello";
            [MemberPrivate] public string[] Member3 { get; set; } = { "hello", "hello2" };

            [MemberPrivate]
            public int Plus(int a, int b)
            {
                return a + b;
            }
        }

        public static Fuga CreateFuga()
        {
            return new Fuga();
        }

        [Theory]
        [InlineData("fun main() let fuga = create_fuga() ret fuga.member1 end ", 12)]
        [InlineData("fun main() let fuga = create_fuga() fuga.member1 = 20 ret fuga.member1 end ", 20)]
        [InlineData("fun main() let fuga = create_fuga() ret fuga.member2 end ", "hello")]
        [InlineData("fun main() let fuga = create_fuga() fuga.member2 = \"hello2\" ret fuga.member2 end ", "hello2")]
        [InlineData("fun main() let fuga = create_fuga() ret fuga.member3[0] end ", "hello")]
        [InlineData("fun main() let fuga = create_fuga() fuga.member3[0] = \"hello2\" ret fuga.member3[0] end ",
            "hello2")]
        [InlineData("fun main() let fuga = create_fuga() ret fuga.plus(2, 5) end ", 7)]
        public void AccessibilityThrowTest<T>(string str, T expected)
        {
            Assert.Throws<MemberAccessException>(() => RunReturnCheck(str, expected));
        }
    }
}