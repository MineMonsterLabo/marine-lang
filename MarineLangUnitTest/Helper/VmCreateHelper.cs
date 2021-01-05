using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using MarineLang.BuildInObjects;
using MarineLang.LexicalAnalysis;
using MarineLang.Streams;
using MarineLang.SyntaxAnalysis;
using MarineLang.VirtualMachines;
using MarineLang.VirtualMachines.Attributes;

namespace MarineLangUnitTest.Helper
{
    public static class VmCreateHelper
    {
        public static HighLevelVirtualMachine Create(string str)
        {
            var lexer = new LexicalAnalyzer();
            var parser = new SyntaxAnalyzer();

            var tokens = lexer.GetTokens(str).ToArray();
            var tokenStream = TokenStream.Create(tokens);
            var parseResult = parser.Parse(tokens);
            if (parseResult.IsError)
                return null;
            var vm = new HighLevelVirtualMachine();

            vm.SetProgram(parseResult.Value);
            vm.GlobalFuncRegister(typeof(VmCreateHelper).GetMethod(nameof(Ret123)));
            vm.GlobalFuncRegister(typeof(VmCreateHelper).GetMethod(nameof(Hello)));
            vm.GlobalFuncRegister(typeof(VmCreateHelper).GetMethod(nameof(Plus)));
            vm.GlobalFuncRegister(typeof(VmCreateHelper).GetMethod(nameof(Two)));
            vm.GlobalFuncRegister(typeof(VmCreateHelper).GetMethod(nameof(Not)));
            vm.GlobalFuncRegister(typeof(VmCreateHelper).GetMethod(nameof(InvokeInt)));
            vm.GlobalFuncRegister(typeof(VmCreateHelper).GetMethod(nameof(CreateHoge)));
            vm.GlobalFuncRegister(typeof(VmCreateHelper).GetMethod(nameof(CreateFuga)));
            vm.GlobalFuncRegister(typeof(VmCreateHelper).GetMethod(nameof(CreatePiyo)));
            vm.GlobalFuncRegister(typeof(VmCreateHelper).GetMethod(nameof(Wait5)));
            vm.GlobalFuncRegister(typeof(VmCreateHelper).GetMethod(nameof(WaitWait5)));
            vm.GlobalVariableRegister("hoge", new Hoge());
            vm.GlobalVariableRegister("fuga", new Fuga());
            vm.GlobalVariableRegister("piyo", new Piyo());
            vm.GlobalVariableRegister("names", new string[] {"aaa", "bbb"});
            vm.GlobalVariableRegister("namess", new string[][]
            {
                new string[] {"ccc", "ddd"},
                new string[] {"xxx", "yyy"},
            });

            vm.Compile();

            return vm;
        }

        public class Hoge
        {
            public bool flag;
            public string[] Names { get; } = new string[] {"rrr", "qqq"};
            public string this[string index]
            {
                get => index;
            }
            public Dictionary<string, string> Dict => new Dictionary<string, string> { { "hoge", "fuga" } };
            public string Name { get; set; } = "this is the pen";
            public int PlusOne(int x) => x + 1;
            public Hoge GetThis() => this;
        }

        public static Hoge CreateHoge()
        {
            return new Hoge();
        }

        [DefaultMemberAllPrivate]
        public class Fuga
        {
            [MemberPublic] public int member1 = 12;
            [MemberPublic] public string Member2 { get; set; } = "hello";
            [MemberPublic] public string[] Member3 { get; set; } = {"hello", "hello2"};

            [MemberPublic]
            public int Plus(int a, int b)
            {
                return a + b;
            }

            [MemberPublic]
            public int DefaultAndRef(ref int a, int b = 1234)
            {
                return a += b;
            }
        }

        public static Fuga CreateFuga()
        {
            return new Fuga();
        }

        public class Piyo
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

        public static Piyo CreatePiyo()
        {
            return new Piyo();
        }

        [SuppressMessage("Usage", "xUnit1013:Public method should be marked as test",
            Justification = "<保留中>")]
        public static void Hello()
        {
        }

        public static int Ret123()
        {
            return 123;
        }

        public static int Plus(int a, int b)
        {
            return a + b;
        }

        public static int Two(int a)
        {
            return a * 2;
        }

        public static bool Not(bool a)
        {
            return !a;
        }

        public static int InvokeInt(ActionObject actionObject, int val)
        {
            return actionObject.InvokeGeneric<int>(val).Value;
        }

        public static IEnumerator<int> Wait5()
        {
            return Enumerable.Range(1, 5).GetEnumerator();
        }

        public static IEnumerator<IEnumerator<int>> WaitWait5()
        {
            yield return Wait5();
        }
    }
}