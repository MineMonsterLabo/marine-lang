using System.Collections.Generic;
using System.Linq;
using MarineLang.Models;
using MarineLang.Models.Errors;
using MarineLang.SyntaxAnalysis;
using MarineLang.VirtualMachines.Dumps.Models;

namespace MarineLang.CodeAnalysis
{
    public class CompletionContext
    {
        public const int OrderFunction = 0;
        public const int OrderFunctionParameter = 1;
        public const int OrderFunctionType = 2;
        public const int OrderGlobalFunction = 3;
        public const int OrderGlobalVariable = 4;
        public const int OrderKeyword = 5;

        private SyntaxParseResult _result;

        public bool IsSuccess => _result.IsOk;

        public CodeAnalyzer CodeAnalyzer { get; }

        public MarineDumpModel MarineDumpModel { get; }

        public CompletionContext(SyntaxParseResult result, MarineDumpModel dumpModel)
        {
            _result = result;

            CodeAnalyzer = new CodeAnalyzer(result);
            MarineDumpModel = dumpModel;
        }

        public CompletionContext(SyntaxParseResult result, CodeAnalyzer codeAnalyzer)
        {
            _result = result;

            CodeAnalyzer = codeAnalyzer;
            CodeAnalyzer.SetResult(result);
        }

        public IEnumerable<CompletionItem> GetCompletions(Position position,
            CompletionFilterFlags flags = CompletionFilterFlags.All)
        {
            CodeAnalyzer.ExecuteAnalyze(position);

            var list = new List<CompletionItem>();
            if (CodeAnalyzer.CurrentStatement != null)
            {
                var fileFunctions = CodeAnalyzer.FuncDefinitions.Select(e =>
                    new CompletionItem(e.funcName, string.Empty, string.Empty, OrderFunction,
                        CompletionFilterFlags.Function));
                list.AddRange(fileFunctions);

                var parameters = CodeAnalyzer.CurrentFuncParameters.Select(e => new CompletionItem(e.VarName,
                    string.Empty, string.Empty, OrderFunctionParameter, CompletionFilterFlags.FunctionParameter));
                list.AddRange(parameters);

                if (MarineDumpModel != null)
                {
                    var types = MarineDumpModel.Types.Select(e =>
                        new CompletionItem(e.Key, string.Empty, string.Empty, OrderFunctionType,
                            CompletionFilterFlags.Type));
                    list.AddRange(types);

                    var globalFunctions = MarineDumpModel.GlobalMethods.Select(e =>
                        new CompletionItem(e.Key, string.Empty, string.Empty, OrderGlobalFunction,
                            CompletionFilterFlags.GlobalFunction));
                    list.AddRange(globalFunctions);

                    var globalVariables = MarineDumpModel.GlobalVariables.Select(e =>
                        new CompletionItem(e.Key, string.Empty, string.Empty, OrderGlobalVariable,
                            CompletionFilterFlags.GlobalVariable));
                    list.AddRange(globalVariables);
                }

                list.Add(new CompletionItem("let", string.Empty, string.Empty, OrderKeyword,
                    CompletionFilterFlags.Keyword));
                list.Add(new CompletionItem("ret", string.Empty, string.Empty, OrderKeyword,
                    CompletionFilterFlags.Keyword));
                list.Add(new CompletionItem("yield", string.Empty, string.Empty, OrderKeyword,
                    CompletionFilterFlags.Keyword));
                list.Add(new CompletionItem("true", string.Empty, string.Empty, OrderKeyword,
                    CompletionFilterFlags.Keyword));
                list.Add(new CompletionItem("false", string.Empty, string.Empty, OrderKeyword,
                    CompletionFilterFlags.Keyword));
                list.Add(new CompletionItem("null", string.Empty, string.Empty, OrderKeyword,
                    CompletionFilterFlags.Keyword));
            }

            return list.Where(e => flags.HasFlag(e.Flags));
        }

        public IEnumerable<ParseErrorInfo> GetErrorInfos()
        {
            return _result.parseErrorInfos.ToArray();
        }
    }
}