using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using MarineLang.BuildInObjects;
using MarineLang.LexicalAnalysis;
using MarineLang.MacroPlugins;
using MarineLang.PresetMacroPlugins;
using MarineLang.SyntaxAnalysis;
using MarineLang.VirtualMachines;
using MarineLang.VirtualMachines.Attributes;
using MineUtil;

namespace MarineLangUnitTest.Helper
{
    public static class VmCreateHelper
    {
        public static HighLevelVirtualMachine Create(string str)
        {
            var lexer = new LexicalAnalyzer();

            var pluginContainer = new PluginContainer();
            pluginContainer.AddFuncDefinitionPlugin("include", new IncludePlugin());
            pluginContainer.AddExprPlugin("constEval", new ConstExprPlugin());

            var parser = new SyntaxAnalyzer(pluginContainer);

            var tokens = lexer.GetTokens(str).ToArray();
            var parseResult = parser.Parse(tokens);
            if (parseResult.IsError)
                throw new Exception(string.Concat(parseResult.parseErrorInfos.Select(x => x.FullErrorMessage)));
            var vm = CreateVM();

            vm.LoadProgram(new MarineProgramUnit(parseResult.programAst));
            vm.Compile();

            return vm;
        }

        public static HighLevelVirtualMachine CreateVM()
        {
            var vm = new HighLevelVirtualMachine();

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
            vm.GlobalVariableRegister("over_load", new OverLoad());
            vm.GlobalVariableRegister("optional", new Optional());
            vm.GlobalVariableRegister("hoge", new Hoge());
            vm.GlobalVariableRegister("fuga", new Fuga());
            vm.GlobalVariableRegister("piyo", new Piyo());
            vm.GlobalVariableRegister("names", new string[] { "aaa", "bbb" });
            vm.GlobalVariableRegister("namess", new string[][]
            {
                new string[] {"ccc", "ddd"},
                new string[] {"xxx", "yyy"},
            });

            StaticType.Reset();
            vm.StaticTypeRegister(typeof(StaticType));
            vm.StaticTypeRegister(typeof(Constructor));
            vm.StaticTypeRegister<OpSample1>();
            vm.StaticTypeRegister<TestColor>();

            return vm;
        }

        public static uint ParseAndLoad(this HighLevelVirtualMachine vm, string str, params string[] namespaceStrings)
        {
            var lexer = new LexicalAnalyzer();
            var parser = new SyntaxAnalyzer();

            var tokens = lexer.GetTokens(str).ToArray();
            var parseResult = parser.Parse(tokens);
            return vm.LoadProgram(new MarineProgramUnit(namespaceStrings, parseResult.programAst));
        }

        public class Hoge
        {
            public int count = 10;
            public int Count = 100;
            public bool flag;
            public bool Flag;
            public bool flag2 => false;
            public bool Flag2 => true;
            public bool FlagTest;
            public string[] Names { get; } = new string[] {"rrr", "qqq"};

            public string this[string index]
            {
                get => index;
            }

            public Dictionary<string, string> Dict => new Dictionary<string, string> {{"hoge", "fuga"}};
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

        public class OverLoad
        {
            public string Hoge(int a, int b) => "int_int";
            public string Hoge(int a, int? b) => "int_nullable";
            public string Hoge(int a, float b) => "int_float";
            public string Hoge(float a, int b) => "float_int";
            public string Hoge(object a) => "object";
            public string Hoge(int a) => "int";
            public string Hoge(double a) => "double";
            public string Hoge(object a, object b, object c = null) => "object_object_default_object";
            public string Hoge(int a, int b, int c, int? d = null) => "int_int_int_nullable_int";
        }

        public class Optional
        {
            public string Hoge(int a, int b, float c = 10.5f) => $"{a},{b},{c}";
            public string Hoge(int a, float b, float c = 11.5f) => $"{a},{b},{c}";
            public string Hoge(object a) => "object";
        }

        public static class StaticType
        {
            public static string Name => "aaa";

            public static string field = "Hello field!!";
            public static int field2 = 50;
            public static int Field2 = 150;

            public static string Field { get; set; }= "hello";

            public static void Reset()
            {
                field = "Hello field!!";
                field2 = 50;
                Field2 = 150;
                Field = "hello";
            }

            public static string RetFuncName()
            {
                return "func_name";
            }

            public static int Sum(int a, int b)
            {
                return a + b;
            }
        }

        public class Constructor
        {
            public string str;

            public Constructor()
            {
                str = "aaa";
            }

            public Constructor(int a)
            {
                str = "bbb";
            }

            public Constructor(int a, int b)
            {
                str = "ccc";
            }
        }

        public class OpSample1
        {
            public int v;

            public OpSample1(int v)
            {
                this.v = v;
            }

            public static OpSample1 operator +(OpSample1 a, OpSample1 b)
            {
                return new OpSample1(a.v + b.v);
            }

            public static OpSample1 operator +(OpSample1 a, int b)
            {
                return new OpSample1(a.v + b);
            }

            public static OpSample1 operator -(OpSample1 a, int b)
            {
                return new OpSample1(a.v - b);
            }

            public static OpSample1 operator *(OpSample1 a, int b)
            {
                return new OpSample1(a.v * b);
            }

            public static OpSample1 operator /(OpSample1 a, int b)
            {
                return new OpSample1(a.v / b);
            }
        }

        public enum TestColor
        {
            Red,
            Green,
            Blue
        }

        public class SequenceLogger
        {
            public string Log { get; private set; } = "";
            public int Hook(int v, int order)
            {
                Log += order;
                return v;
            }

            public bool Hook(bool v, int order)
            {
                Log += order;
                return v;
            }
        }

        public class Generic
        {
            public T Id<T>(T v) => v;
            public string GetType<T>() => typeof(T).Name;
            public string GetType<T>(int aaa) => typeof(T).Name + "," + aaa;
            public string GetType<T,TT>(TT aaa) => typeof(T).Name + "," + typeof(TT).Name + "," + aaa;
            public string GetType<T, TT>() => typeof(T).Name + "," + typeof(TT).Name;

            public static string GetType2<T>() => typeof(T).Name;

            public static T IdStatic<T>(T v) => v;

            public string Test<T, TT>(T _, TT __) 
                => "2 Generic "+_.GetType().Name + ":" + __.GetType().Name;

            public string Test<T>(T _, float __)
                => "1 Generic " + _.GetType().Name + ":" + "Single";

            public string Test(float _, float __)
                => "0 Generic Single:Single";
        }
    }
}