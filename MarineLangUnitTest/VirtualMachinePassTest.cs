using MarineLang.BuiltInTypes;
using MarineLang.LexicalAnalysis;
using MarineLang.Streams;
using MarineLang.SyntaxAnalysis;
using MarineLang.VirtualMachines;
using System.Linq;
using Xunit;

namespace MarineLangUnitTest
{
    public class VirtualMachinePassTest
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
            vm.GlobalFuncRegister(typeof(VirtualMachinePassTest).GetMethod("create_hoge"));
            vm.GlobalVariableRegister("hoge", new Hoge());

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
            public string Name { get; set; } = "this is the pen";
            public int PlusOne(int x) => x + 1;
        }

        public static Hoge create_hoge() { return new Hoge(); }

        public static void hello() { }
        public static int ret_123() { return 123; }
        public static int plus(int a, int b) { return a + b; }
        public static int two(int a) { return a * 2; }
        public static bool not(bool a) { return !a; }

        [Theory]
        [InlineData("fun main() hello() end")]
        [InlineData(@"
fun main() foo_bar() end 
fun foo_bar() hello() end"
        )]
        public void CallMarineLangFunc(string str)
        {
            RunReturnCheck(str, new UnitType());
        }

        [Theory]
        [InlineData("fun main() ret 123 end")]
        [InlineData("fun main() ret 123 ret 115 end")]
        [InlineData("fun main() ret 123 hogehoge() end")]
        [InlineData("fun main() hello() ret 123 end")]
        [InlineData("fun main() ret ret_123() end")]
        [InlineData(@"
fun main() ret fuga() end
fun fuga() ret 123 end
")]
        public void CallMarineLangFuncRetInt(string str)
        {
            RunReturnCheck(str, 123);
        }

        [Theory]
        [InlineData("fun main() ret false end", false)]
        [InlineData("fun main() ret true end", true)]
        public void CallMarineLangFuncRetBool(string str, bool flag)
        {
            RunReturnCheck(str, flag);
        }

        [Theory]
        [InlineData("fun main() ret 'c' end", 'c')]
        [InlineData("fun main() ret '\\'' end", '\'')]
        [InlineData("fun main() ret '\\\\' end", '\\')]
        [InlineData("fun main() ret '\\n' end", '\n')]
        [InlineData("fun main() ret '\\t' end", '\t')]
        [InlineData("fun main() ret '\\r' end", '\r')]
        [InlineData("fun main() ret 'あ' end", 'あ')]
        public void CallMarineLangFuncRetChar(string str, char c)
        {
            RunReturnCheck(str, c);
        }

        [Theory]
        [InlineData("fun main() ret \"hoge\" end", "hoge")]
        [InlineData("fun main() ret \"\\\"\" end", "\"")]
        [InlineData("fun main() ret \"\\\\\" end", "\\")]
        [InlineData("fun main() ret \"\\n\" end", "\n")]
        [InlineData("fun main() ret \"\\t\" end", "\t")]
        [InlineData("fun main() ret \"\\r\" end", "\r")]
        [InlineData("fun main() ret \"あ\" end", "あ")]
        [InlineData("fun main() ret \"あ\\rhoge\\\"\" end", "あ\rhoge\"")]
        public void CallMarineLangFuncRetString(string str, string expected)
        {
            RunReturnCheck(str, expected);

        }

        [Theory]
        [InlineData("fun main() ret 1.2 end", 1.2f)]
        [InlineData("fun main() ret 5.0 end", 5.0f)]
        [InlineData("fun main() ret 3.3333214 end", 3.3333214f)]
        public void CallMarineLangFuncRetFloat(string str, float expected)
        {
            RunReturnCheck(str, expected);
        }

        [Theory]
        [InlineData("fun main() ret plus(3,2) end", 5)]
        [InlineData("fun main() ret two(3) end", 6)]
        [InlineData("fun main() ret plus(1,two(two(3))) end", 13)]
        public void CallCsharpFuncWithArgs(string str, int expected)
        {
            RunReturnCheck(str, expected);
        }

        [Theory]
        [InlineData("fun main() ret not(not(false)) end", false)]
        [InlineData("fun main() ret not(not(true)) end", true)]
        public void CallCsharpFuncWithArgs2(string str, bool expected)
        {
            RunReturnCheck(str, expected);
        }

        [Theory]
        [InlineData("fun main() ret f(1) end fun f(a) a=4 ret a end", 4)]
        [InlineData("fun main() let abc_d=false ret abc_d end", false)]
        [InlineData("fun main() let hoge=5 hoge=3 ret hoge end", 3)]
        [InlineData("fun main() let left=5 let right=3 ret plus(left,right) end", 8)]
        [InlineData("fun main() let str=\"あいうえお\"ret str end", "あいうえお")]
        [InlineData("fun main() let ccc = '$'let aa=\"abab\" ret ccc end", '$')]
        [InlineData(@"
fun main() let a = 13 ret plus(f(),a) end
fun f() let a=3 ret a end
", 16)]
        public void Variable<T>(string str, T expected)
        {
            RunReturnCheck(str, expected);
        }

        [Theory]
        [InlineData("fun main() ret id(4) end fun id(a) ret a end", 4)]
        [InlineData("fun main() ret f(\"abc\",'c',4,3.5) end fun f(a,b,c,d) ret a end", "abc")]
        [InlineData("fun main() ret f(\"abc\",'c',4,3.5) end fun f(a,b,c,d) ret b end", 'c')]
        [InlineData("fun main() ret f(\"abc\",'c',4,3.5) end fun f(a,b,c,d) ret c end", 4)]
        [InlineData("fun main() ret f(\"abc\",'c',4,3.5) end fun f(a,b,c,d) ret d end", 3.5f)]
        public void CallMarineFuncWithArgs<T>(string str, T expected)
        {
            RunReturnCheck(str, expected);
        }

        [Theory]
        [InlineData("fun main(a,b) ret plus(a,b) end ")]
        public void CallMarineFuncWithArgs2(string str)
        {
            var vm = VmCreateHelper(str);

            Assert.NotNull(vm);

            var ret = vm.Run<int>("main", 12, 8);

            Assert.Equal(20, ret);
        }

        [Theory]
        [InlineData("fun main() ret 1 + 5 end", 6)]
        [InlineData("fun main() ret \"a\" + \"b\" + \"c\" end", "abc")]
        [InlineData("fun main() ret 3.5 + 1.5 end", 3.5f + 1.5f)]
        [InlineData("fun main() ret 2 * 3 + 10 / 2 end", 11)]
        [InlineData("fun main() ret 2 / 8 end", 0)]
        [InlineData("fun main() ret 5-4-3 end", 5 - 4 - 3)]
        [InlineData("fun main() ret 5 == 4 end", false)]
        [InlineData("fun main() ret 5 != 5-1 end", true)]
        [InlineData("fun main() ret true || false end", true)]
        [InlineData("fun main() ret true && false end", false)]
        [InlineData("fun main() ret true && false || true end", true)]
        [InlineData("fun main() ret 1 > 0 end", true)]
        [InlineData("fun main() ret 10 > 6 + 5 end", false)]
        [InlineData("fun main() ret 100.0 / 10.0 < 6.0 + 5.0 end", true)]
        [InlineData("fun main() ret 10 > 10 end", false)]
        [InlineData("fun main() ret 10 < 10 end", false)]
        [InlineData("fun main() ret 10 >= 10 end", true)]
        [InlineData("fun main() ret 10 <= 10 end", true)]
        [InlineData("fun main() ret 10 <= 9 end", false)]
        public void Operator<T>(string str, T expected)
        {
            RunReturnCheck(str, expected);
        }

        [Theory]
        [InlineData("fun main() ret (4+5)*2 end ", (4 + 5) * 2)]
        [InlineData("fun main() ret (4+5)*(3-7) end ", (4 + 5) * (3 - 7))]
        [InlineData("fun main() ret ((plus((8),(2)))) end ", 10)]
        public void ParenExpr<T>(string str, T expected)
        {
            RunReturnCheck(str, expected);
        }

        [Theory]
        [InlineData("fun main() ret if(1==2-1) {\"ok\"} end ", "ok")]
        [InlineData("fun main() if(true) {ret 1 ret 2} end ", 1)]
        [InlineData("fun main() ret if(1!=2-1) {\"ok\"} else {\"no\"}end ", "no")]
        [InlineData(
@"
fun main() 
    let result = sum(0, 100)
    ret result
end

fun sum(min, max)  
    ret 
        if min == max 
        {min} 
        else {
	 min + sum(min + 1, max)
        }
end
"
            , 5050)]
        public void IfExpr<T>(string str, T expected)
        {
            RunReturnCheck(str, expected);
        }

        [Theory]
        [InlineData("fun main() ret 123.to_string() end", "123")]
        [InlineData("fun main() ret 123.to_string().to_string() end", "123")]
        [InlineData("fun main() ret \"HogeFuga\".to_lower()==\"hogefuga\" end", true)]
        [InlineData("fun main() ret f().to_string(\"000\") end fun f() ret 4+8 end", "012")]
        public void InstanceFuncCall<T>(string str, T expected)
        {
            RunReturnCheck(str, expected);
        }

        [Theory]
        [InlineData("fun main() ret create_hoge().flag end", false)]
        [InlineData("fun main() ret create_hoge().flag.to_string() end", "False")]
        [InlineData("fun main() ret create_hoge().flag||true end", true)]
        [InlineData("fun main() ret create_hoge().name end", "this is the pen")]
        public void InstanceField<T>(string str, T expected)
        {
            RunReturnCheck(str, expected);
        }

        [Theory]
        [InlineData("fun main() let hoge = create_hoge() hoge.flag=4!=5 ret hoge.flag end", true)]
        [InlineData("fun main() let hoge = create_hoge() hoge.name = 5.to_string() ret hoge.name end", "5")]
        public void InstanceFieldAssignment<T>(string str, T expected)
        {
            RunReturnCheck(str, expected);
        }


        [Theory]
        [InlineData(@"
fun main() 
    let total = 0
    let max = 100
    let now = 0
    while now <= max {
        total = total + now
        now = now + 1
    }
    ret total
end
", 5050)]
        public void WhileStatement<T>(string str, T expected)
        {
            RunReturnCheck(str, expected);
        }

        [Theory]
        [InlineData(@"
fun main() 
    let total = 0
    for i =0 , 100 , 1 {
        total = total + i
    }
    ret total
end
", 5050)]
        public void ForStatement<T>(string str, T expected)
        {
            RunReturnCheck(str, expected);
        }

        [Theory]
        [InlineData(@"
fun main() 
    let a = 5 
    let c = if true { 
        let b = 3 
        let d = a+b 
        for i = 1 , 10 , 1{
            let e = d + 1
            d = e
        }
        d
    }
    ret c 
end", 18)]
        public void NestLet<T>(string str, T expected)
        {
            RunReturnCheck(str, expected);
        }
        [Theory]
        [InlineData("fun main()  hoge.name = \"gg\"  ret hoge.name end", "gg")]
        [InlineData("fun main() ret hoge.plus_one(5) end", 6)]
        public void GlobalVariable<T>(string str, T expected)
        {
            RunReturnCheck(str, expected);
        }
    }
}
