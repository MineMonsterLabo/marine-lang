using MarineLang;
using System;
using System.Linq;
using System.Reflection;
using System.IO;
using MarineLang.LexicalAnalysis;
using MarineLang.SyntaxAnalysis;
using MarineLang.Streams;

namespace Example
{
    class Program
    {
        static VirtualMachine vm;

        static void Main(string[] args)
        {
            vm = new VirtualMachine();
            vm.Register(typeof(Program).GetMethod("hello", BindingFlags.Static | BindingFlags.NonPublic));

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

            var parserResult = new Parser().Parse(tokenStream);

            if (parserResult.isError)
            {
                Console.WriteLine("パースエラー");
                return;
            }

            vm.SetProgram(parserResult.value);
        }

        static void hello()
        {
            Console.WriteLine("hello world");
        }
    }
}
