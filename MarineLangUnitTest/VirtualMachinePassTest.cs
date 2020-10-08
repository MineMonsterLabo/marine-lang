using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using MarineLang.BuildInObjects;
using MarineLang.BuiltInTypes;
using MarineLang.VirtualMachines.Attributes;
using MarineLangUnitTest.Helper;
using Xunit;

namespace MarineLangUnitTest
{
    public class VirtualMachinePassTest
    {
        internal void RunReturnCheck<RET>(string str, RET expected)
        {
            var vm = VmCreateHelper.Create(str);

            Assert.NotNull(vm);

            var ret = vm.Run<RET>("main");

            Assert.Equal(expected, ret);
        }

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
            var vm = VmCreateHelper.Create(str);

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
        [InlineData("fun main() ret 10*6/5 end", 12)]
        [InlineData("fun main() ret 5%2 end", 1)]
        [InlineData("fun main() ret 5%2*2 end", 2)]
        [InlineData("fun main() ret 6%2 end", 0)]
        public void BinaryOperator<T>(string str, T expected)
        {
            RunReturnCheck(str, expected);
        }

        [Theory]
        [InlineData("fun main() ret !false end", true)]
        [InlineData("fun main() ret !!false end", false)]
        [InlineData("fun main() ret -5 end", -5)]
        [InlineData("fun main() ret -5.3 end", -5.3f)]
        [InlineData("fun main() ret 10.0-5.3 end", 10.0f - 5.3f)]
        [InlineData("fun main() ret !(((-8)+(-2))==-10) end", false)]
        public void UnaryOperator<T>(string str, T expected)
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
        [InlineData("fun main()  hoge.get_this().name = \"gg\"  ret hoge.name end", "gg")]
        [InlineData("fun main() ret hoge.plus_one(5) end", 6)]
        public void GlobalVariable<T>(string str, T expected)
        {
            RunReturnCheck(str, expected);
        }

        [Theory]
        [InlineData("fun main() ret names[0] end", "aaa")]
        [InlineData("fun main() ret names[2-1] end", "bbb")]
        [InlineData("fun main() ret namess[1][0] end", "xxx")]
        [InlineData("fun main() ret hoge.names[1] end", "qqq")]
        [InlineData("fun main() names[1] = \"SAO\" ret names[1] end", "SAO")]
        [InlineData("fun main() hoge.names[1] = \"AAA\" ret hoge.names[1] end", "AAA")]
        [InlineData("fun main() hoge.get_this().get_this().names[1] = \"AAA\" ret hoge.get_this().names[1] end", "AAA")]
        public void Indexer<T>(string str, T expected)
        {
            RunReturnCheck(str, expected);
        }

        [Theory]
        [InlineData("fun main() ret [0,1,3] end", new object[] {0, 1, 3})]
        [InlineData("fun main() ret [7;3] end", new object[] {7, null, null})]
        public void ArrayLiteral<T>(string str, T expected)
        {
            RunReturnCheck(str, expected);
        }

        [Theory]
        [InlineData("fun main() for i=0,10000,1{5} end")]
        [InlineData("fun main() for i=0,10000,1{ret_123()} end")]
        public void NoUseExpr(string str)
        {
            RunReturnCheck(str, new UnitType());
        }

        [Theory]
        [InlineData("fun main() ret if(true) {} end ")]
        [InlineData("fun main() ret if(true) {let a=5} end ")]
        [InlineData("fun main() ret if(false) {4}else{} end ")]
        public void UnitIf(string str)
        {
            RunReturnCheck(str, new UnitType());
        }

        [Theory]
        [InlineData(@"
fun main()
    let aa = 10
    let bb = 5
    let action = action_object_generator.generate(""hoge"",[bb,aa])
    ret invoke_int(action,8) + aa
end

fun hoge(action,x)
    ret action.get(0) + x-action.get(1)
end
")]
        public void ActionObjectCall(string str)
        {
            RunReturnCheck(str, 5 + 8);
        }

        [Theory]
        [InlineData(@"
fun main()
    let aa = 10
    let bb = 5
    let action = {|x| 
        bb=bb+1 
        ret bb + x - aa+invoke_int({ |y| ret x*y }, 2)
    }
    ret invoke_int(action,8)+aa
end
")]
        [InlineData(@"
fun main()
    let aa = 10
    let bb = 5
    let action = {|x| 
        bb=bb+1 
        ret bb + x - aa+{ |y| ret x*y }.invoke([2])
    }
    ret action.invoke([8])+aa
end
")]
        public void ActionObjectCall2(string str)
        {
            RunReturnCheck(str, 6 + 8 + 8 * 2);
        }

        [Theory]
        [InlineData(@"
fun main()
    ret {|f| 
        ret 
            { |x| ret f.invoke( [{ |y| ret x.invoke([x]).invoke([y]) }] ) }
            .invoke( [{|x| ret f.invoke( [{ |y| ret x.invoke([x]).invoke([y]) }] ) }] )   
    }.invoke([
        { |f| 
            ret { |n| 
                ret if n==0 {1} else {n * f.invoke([n - 1]) } 
            }
        } 
    ]).invoke([5])
end
")]
        [InlineData(@"
fun main()ret{|f|ret{|x|ret f.invoke([{|y|ret x.invoke([x]).invoke([y])}])}.invoke([{|x|ret f.invoke([{|y|ret x.invoke([x]).invoke([y])}])}])}.invoke([{|f|ret{|n|ret if n==0{1}else{n*f.invoke([n-1])}}}]).invoke([5])end
")]
        public void ActionObjectCall3(string str)
        {
            RunReturnCheck(str, 120);
        }

        [Theory]
        [InlineData("fun main() yield ret 4 end ", 4)]
        [InlineData("fun main() yield ret hoge() + 1 end fun hoge() yield ret 5 end ", 6)]
        public void YieldTest<T>(string str, T expected)
        {
            var vm = VmCreateHelper.Create(str);

            Assert.NotNull(vm);

            var ret = vm.Run<IEnumerable<object>>("main");

            var value = ret.ToArray().Last();

            Assert.Equal(expected, value);
        }

        [Theory]
        [InlineData("fun main() ret wait5().await end ", 5)]
        [InlineData("fun main() ret wait_wait5().await.await end ", 5)]
        [InlineData("fun main() ret wait_wait5().await.await+1 end ", 6)]
        [InlineData("fun main() ret hoge() end fun hoge() ret wait5().await end ", 5)]
        public void AwaitTest<T>(string str, T expected)
        {
            var vm = VmCreateHelper.Create(str);

            Assert.NotNull(vm);

            var ret = vm.Run<IEnumerable<object>>("main");

            var value = ret.ToArray().Last();

            Assert.Equal(expected, value);
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
        public void AccessibilityTest<T>(string str, T expected)
        {
            RunReturnCheck(str, expected);
        }
    }
}