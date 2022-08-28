using System.Collections.Generic;
using MarineLang.MacroPlugins;
using MarineLang.Models;
using MarineLang.Models.Asts;
using MarineLang.Models.Errors;
using MarineLang.Inputs;
using MarineLang.SyntaxAnalysis;
using MarineLang.VirtualMachines;
using MineUtil;

namespace MarineLang.PresetMacroPlugins
{
    public class ConstExprPlugin : IExprMacroPlugin
    {
        public IResult<ExprAst, ParseErrorInfo> Replace(SyntaxParser marineParser, List<Token> tokens)
        {
            var vm = new HighLevelVirtualMachine();
            return
                marineParser.ParseExpr()(TokenInput.Create(tokens.ToArray()))
                .Result.Select(exprAst =>
                {
                    var programAst = ProgramAst.Create(
                            new[]{
                                FuncDefinitionAst.Create(
                                    "main",
                                    new VariableAst[] { },
                                    new StatementAst[]{ReturnAst.Create(exprAst) }
                                )
                            }
                        );
                    vm.LoadProgram(new MarineProgramUnit(programAst));
                    vm.Compile();
                    return ValueAst.Create(vm.Run("main").Eval());
                });
        }
    }
}
