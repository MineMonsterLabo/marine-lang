using System;
using System.Reflection;
using System.IO;
using MarineLang.LexicalAnalysis;
using MarineLang.SyntaxAnalysis;
using MarineLang.VirtualMachines;

namespace Example
{
    class Program
    {
        static HighLevelVirtualMachine vm;

        static void Main(string[] args)
        {
            vm = new HighLevelVirtualMachine();
            vm.GlobalFuncRegister(typeof(Program).GetMethod(nameof(Print), BindingFlags.Static | BindingFlags.NonPublic));
            vm.GlobalFuncRegister(typeof(Program).GetMethod(nameof(Plus), BindingFlags.Static | BindingFlags.NonPublic));
            vm.GlobalFuncRegister(typeof(Program).GetMethod(nameof(ToString), BindingFlags.Static | BindingFlags.NonPublic));

            while (true)
            {
                var str = Console.ReadLine();
                if (str.StartsWith("\\f"))
                {
                    var filePath = str.Replace("\\f", "");
                    filePath = filePath.Replace(" ", "");
                    SetProgram(File.ReadAllText(filePath));
                }
                else if (str.StartsWith("\\c"))
                {
                    var marineFuncName = str.Replace("\\c", "");
                    marineFuncName = marineFuncName.Replace(" ", "");
                    vm.Run<object>(marineFuncName);
                }
                else
                    SetProgram(str);
            }
        }

        static void SetProgram(string code)
        {
            var lexer = new LexicalAnalyzer();

            var tokens = lexer.GetTokens(code);
            Console.WriteLine("トークン解析結果");
            foreach (var token in tokens)
            {
                Console.WriteLine(token.text + " : " + token.tokenType);
            }
            Console.WriteLine("");

            var parserResult = new SyntaxAnalyzer().Parse(tokens);

            if (parserResult.IsError)
            {
                Console.WriteLine("パースエラー");
                Console.WriteLine(parserResult.RawError.FullErrorMessage);
                return;
            }

            vm.LoadProgram(parserResult.RawValue);
            vm.Compile();
        }

        static void Print(string str)
        {
            Console.WriteLine(str);
        }

        static int Plus(int a, int b)
        {
            return a + b;
        }

        static string ToString(int a)
        {
            return a.ToString();
        }
    }
}
