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

        public CompletionContext(SyntaxParseResult result)
        {
            _result = result;
        }

        public void GetTypes(Position position)
        {
        }

        public void GetGlobalFunctions(Position position)
        {
        }

        public void GetFunctions(Position position)
        {
        }

        public void GetGlobalVariables(Position position)
        {
        }

        public void GetVariables(Position position)
        {
        }

        public void GetKeywords(Position position)
        {
        }

        public void GetSnippets(Position position)
        {
        }

        public IEnumerable<ParseErrorInfo> GetErrorInfos()
        {
            return _result.parseErrorInfos.ToArray();
        }
    }
}