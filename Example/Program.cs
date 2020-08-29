using System;
using System.Linq;
using System.Reflection;
using System.IO;
using MarineLang.LexicalAnalysis;
using MarineLang.SyntaxAnalysis;
using MarineLang.Streams;
using MarineLang.VirtualMachines;

namespace Example
{
    class Program
    {
        static HighLevelVirtualMachine vm;

        static void Main(string[] args)
        {
            vm = new HighLevelVirtualMachine();
            vm.GlobalFuncRegister(typeof(Program).GetMethod("print", BindingFlags.Static | BindingFlags.NonPublic));
            vm.GlobalFuncRegister(typeof(Program).GetMethod("plus", BindingFlags.Static | BindingFlags.NonPublic));
            vm.GlobalFuncRegister(typeof(Program).GetMethod("to_string", BindingFlags.Static | BindingFlags.NonPublic));

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
            var lexer = new Lexer();

            var tokens = lexer.GetTokens(code);
            Console.WriteLine("トークン解析結果");
            foreach (var token in tokens)
            {
                Console.WriteLine(token.text + " : " + token.tokenType);
            }
            Console.WriteLine("");

            var tokenStream = TokenStream.Create(tokens.ToArray());

            var parserResult = new SyntaxAnalyzer().Parse(tokenStream);

            if (parserResult.IsError)
            {
                Console.WriteLine("パースエラー");
                Console.WriteLine(parserResult.Error.FullErrorMessage);
                return;
            }

            vm.SetProgram(parserResult.Value);
            vm.Compile();
        }

        static void print(string str)
        {
            Console.WriteLine(str);
        }

        static int plus(int a, int b)
        {
            return a + b;
        }

        static string to_string(int a)
        {
            return a.ToString();
        }
    }
}
