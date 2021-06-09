using System.Collections.Generic;
using MarineLang.MacroPlugins;
using MarineLang.Models;
using MarineLang.Models.Asts;
using MarineLang.Models.Errors;
using MarineLang.Streams;
using MarineLang.SyntaxAnalysis;
using MarineLang.VirtualMachines;
using MineUtil;

namespace MarineLang.PresetMacroPlugins
{
    public class ConstExprPlugin : IExprMacroPlugin
    {
        public IResult<ExprAst, ParseErrorInfo> Replace(MarineParser marineParser, List<Token> tokens)
        {
            var vm = new HighLevelVirtualMachine();
            return
                marineParser.ParseExpr()(TokenStream.Create(tokens.ToArray()))
                .Select(exprAst =>
                {
                    vm.LoadProgram(
                        ProgramAst.Create(
                            new[]{
                                FuncDefinitionAst.Create(
                                    "main",
                                    new VariableAst[] { },
                                    new StatementAst[]{ReturnAst.Create(exprAst) }
                                )
                            }
                        )
                    );
                    vm.Compile();
                    return ValueAst.Create(vm.Run("main").Eval());
                });
        }
    }
}
