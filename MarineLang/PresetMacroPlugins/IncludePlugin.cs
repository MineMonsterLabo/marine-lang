﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using MarineLang.LexicalAnalysis;
using MarineLang.MacroPlugins;
using MarineLang.Models;
using MarineLang.Models.Asts;
using MarineLang.Models.Errors;
using MarineLang.Streams;
using MarineLang.SyntaxAnalysis;
using MineUtil;

namespace MarineLang.PresetMacroPlugins
{
    public class IncludePlugin : IFuncDefinitionMacroPlugin
    {
        public IResult<IEnumerable<FuncDefinitionAst>,ParseErrorInfo> Replace(MarineParser marineParser, List<Token> tokens)
        {
            var str = "";
            foreach (var token in tokens)
            {
                using (var sr = new StreamReader(token.text.Substring(1, token.text.Length - 2)))
                {
                    str += sr.ReadToEnd() + " ";
                }
            }

            var tokens2 = new LexicalAnalyzer().GetTokens(str);

            return
                marineParser.ParseProgram(TokenStream.Create(tokens2.ToArray()))
                .Select(programAst => programAst.funcDefinitionAsts);
        }
    }
}
