using System.Collections.Generic;
using MarineLang.MacroPlugins;
using MarineLang.Models;
using MarineLang.Models.Asts;
using MarineLang.Streams;
using MarineLang.SyntaxAnalysis;
using MarineLang.VirtualMachines;

namespace MarineLang.PresetMacroPlugins
{
    public class ConstExprPlugin : IExprMacroPlugin
    {
        public IParseResult<ExprAst> Replace(MarineParser marineParser, List<Token> tokens)
        {
            var vm = new HighLevelVirtualMachine();
            return
                marineParser.ParseExpr()(TokenStream.Create(tokens.ToArray()))
                .Map(exprAst =>
                {
                    vm.SetProgram(
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
