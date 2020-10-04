using System;
using System.Collections.Generic;
using System.Linq;
using MarineLang.BuildInObjects;
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
            vm.GlobalFuncRegister(typeof(VirtualMachineFailTest).GetMethod("ret_123"));
            vm.GlobalFuncRegister(typeof(VirtualMachineFailTest).GetMethod("hello"));
            vm.GlobalFuncRegister(typeof(VirtualMachineFailTest).GetMethod("plus"));
            vm.GlobalFuncRegister(typeof(VirtualMachineFailTest).GetMethod("two"));
            vm.GlobalFuncRegister(typeof(VirtualMachineFailTest).GetMethod("not"));
            vm.GlobalFuncRegister(typeof(VirtualMachineFailTest).GetMethod("invoke_int"));
            vm.GlobalFuncRegister(typeof(VirtualMachineFailTest).GetMethod("create_hoge"));
            vm.GlobalFuncRegister(typeof(VirtualMachineFailTest).GetMethod("create_fuga"));
            vm.GlobalFuncRegister(typeof(VirtualMachineFailTest).GetMethod("wait5"));
            vm.GlobalFuncRegister(typeof(VirtualMachineFailTest).GetMethod("waitwait5"));
            vm.GlobalVariableRegister("hoge", new Hoge());
            vm.GlobalVariableRegister("names", new string[] {"aaa", "bbb"});
            vm.GlobalVariableRegister("namess", new string[][]
            {
                new string[] {"ccc", "ddd"},
                new string[] {"xxx", "yyy"},
            });

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

        public class Hoge
        {
            public bool flag;
            public string[] Names { get; } = new string[] {"rrr", "qqq"};
            public string Name { get; set; } = "this is the pen";
            public int PlusOne(int x) => x + 1;
            public Hoge GetThis() => this;
        }

        public static Hoge create_hoge()
        {
            return new Hoge();
        }

        public class Fuga
        {
            [MemberPrivate] public int member1 = 12;
            [MemberPrivate] public string Member2 { get; set; } = "hello";
            [MemberPrivate] public string[] Member3 { get; set; } = {"hello", "hello2"};

            [MemberPrivate]
            public int Plus(int a, int b)
            {
                return a + b;
            }
        }

        public static Fuga create_fuga()
        {
            return new Fuga();
        }

        public static void hello()
        {
        }

        public static int ret_123()
        {
            return 123;
        }

        public static int plus(int a, int b)
        {
            return a + b;
        }

        public static int two(int a)
        {
            return a * 2;
        }

        public static bool not(bool a)
        {
            return !a;
        }

        public static int invoke_int(ActionObject actionObject, int val)
        {
            return actionObject.InvokeGeneric<int>(val);
        }

        public static IEnumerator<int> wait5()
        {
            return Enumerable.Range(1, 5).GetEnumerator();
        }

        public static IEnumerator<IEnumerator<int>> waitwait5()
        {
            yield return wait5();
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