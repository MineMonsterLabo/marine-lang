using System.Collections.Generic;
using System.Linq;
using MarineLang.Models;
using MarineLang.Models.Errors;
using MarineLang.SyntaxAnalysis;

namespace MarineLang.CodeAnalysis
{
    public class CompletionContext
    {
        private SyntaxParseResult _result;

        public bool IsSuccess => _result.IsOk;

        public CodeAnalyzer CodeAnalyzer { get; }

        public CompletionContext(SyntaxParseResult result)
        {
            _result = result;

            CodeAnalyzer = new CodeAnalyzer(result);
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
                var parameters = CodeAnalyzer.CurrentFuncParameters.Select(e =>
                    new CompletionItem(e.VarName, string.Empty, string.Empty, 0, CompletionFilterFlags.FuncVariable));
                list.AddRange(parameters);
            }

            return list.Where(e => flags.HasFlag(e.Flags));
        }

        public IEnumerable<ParseErrorInfo> GetErrorInfos()
        {
            return _result.parseErrorInfos.ToArray();
        }
    }
}