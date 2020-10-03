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
            vm.GlobalFuncRegister(typeof(VirtualMachinePassTest).GetMethod("ret_123"));
            vm.GlobalFuncRegister(typeof(VirtualMachinePassTest).GetMethod("hello"));
            vm.GlobalFuncRegister(typeof(VirtualMachinePassTest).GetMethod("plus"));
            vm.GlobalFuncRegister(typeof(VirtualMachinePassTest).GetMethod("two"));
            vm.GlobalFuncRegister(typeof(VirtualMachinePassTest).GetMethod("not"));
            vm.GlobalFuncRegister(typeof(VirtualMachinePassTest).GetMethod("invoke_int"));
            vm.GlobalFuncRegister(typeof(VirtualMachinePassTest).GetMethod("create_hoge"));
            vm.GlobalFuncRegister(typeof(VirtualMachinePassTest).GetMethod("create_fuga"));
            vm.GlobalFuncRegister(typeof(VirtualMachinePassTest).GetMethod("create_piyo"));
            vm.GlobalFuncRegister(typeof(VirtualMachinePassTest).GetMethod("wait5"));
            vm.GlobalFuncRegister(typeof(VirtualMachinePassTest).GetMethod("waitwait5"));
            vm.GlobalVariableRegister("hoge", new VirtualMachinePassTest.Hoge());
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
            public int member1 = 12;
            [MemberPrivate] public string Member2 { get; } = "hello";

            [MemberPrivate]
            public int Plus(int a, int b)
            {
                return a + b;
            }

            public int PublicPlus(int a, int b)
            {
                return a + b;
            }
        }

        public static Fuga create_fuga()
        {
            return new Fuga();
        }

        [DefaultMemberAllPrivate]
        public class Piyo
        {
            [MemberPublic] public int member1 = 256;
            public string Member2 { get; } = "hello";
        }

        public static Piyo create_piyo()
        {
            return new Piyo();
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
        [InlineData("fun main() let fuga = create_fuga() ret fuga.plus(2, 5) end ", 7)]
        [InlineData("fun main() let piyo = create_piyo() ret piyo.member2 end ", "hello")]
        public void AccessibilityThrowTest<T>(string str, T expected)
        {
            Assert.Throws<MemberAccessException>(() => RunReturnCheck(str, expected));
        }
    }
}