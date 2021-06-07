using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MarineLang.BuiltInTypes;
using MarineLang.Models;
using MarineLang.Models.Asts;
using MarineLang.Utils;
using MarineLang.VirtualMachines.MarineILs;

namespace MarineLang.VirtualMachines
{
    public class ILGeneratedData
    {
        public readonly NamespaceTable namespaceTable;
        public readonly IReadOnlyList<IMarineIL> marineILs;

        public ILGeneratedData(NamespaceTable namespaceTable, IReadOnlyList<IMarineIL> marineILs)
        {
            this.namespaceTable = namespaceTable;
            this.marineILs = marineILs;
        }
    }

    public class ActionFuncData
    {
        public readonly string funcName;
        public readonly ActionAst actionAst;
        public readonly string[] captureVarNames;

        public ActionFuncData(string funcName, ActionAst actionAst, string[] captureVarNames)
        {
            this.funcName = funcName;
            this.actionAst = actionAst;
            this.captureVarNames = captureVarNames;
        }
    }

    public class BreakIndex
    {
        public int Index { get; set; }
    }

    public class FuncILIndex
    {
        public int Index { get; set; } = -1;
    }

    public class ILGenerator
    {
        NamespaceTable namespaceTable;
        IReadOnlyDictionary<string, MethodInfo> csharpFuncDict;
        IReadOnlyDictionary<string, Type> staticTypeDict;
        readonly List<IMarineIL> marineILs = new List<IMarineIL>();
        readonly List<ActionFuncData> actionFuncDataList = new List<ActionFuncData>();
        readonly IEnumerable<MarineProgramUnit> marineProgramUnits;

        public ILGenerator(IEnumerable<MarineProgramUnit> marineProgramUnits)
        {
            this.marineProgramUnits = marineProgramUnits;
        }

        public ILGeneratedData Generate(IReadOnlyDictionary<string, MethodInfo> csharpFuncDict,
            IReadOnlyDictionary<string, Type> staticTypeDict, string[] globalVariableNames)
        {
            this.csharpFuncDict = csharpFuncDict;
            this.staticTypeDict = staticTypeDict;
            namespaceTable
                = new NamespaceTable(
                    marineProgramUnits.SelectMany(x=>x.programAst.funcDefinitionAsts)
                        .ToDictionary(x => x.funcName, x => new FuncILIndex())
                  );
            foreach (var marineProgramUnit in marineProgramUnits)
            {
                ProgramILGenerate(marineProgramUnit.programAst, globalVariableNames);
            }
            return new ILGeneratedData(namespaceTable, marineILs);
        }

        void ProgramILGenerate(ProgramAst programAst, string[] globalVariableNames)
        {
            foreach (var funcDefinitionAst in programAst.funcDefinitionAsts)
                FuncDefinitionILGenerate(funcDefinitionAst,
                    new FuncScopeVariables(funcDefinitionAst.args, globalVariableNames));
            for (var i = 0; i < actionFuncDataList.Count; i++)
                ActionFuncILGenerate(actionFuncDataList[i], globalVariableNames);
        }

        void FuncDefinitionILGenerate(FuncDefinitionAst funcDefinitionAst, FuncScopeVariables variables)
        {
            namespaceTable.SetFuncIlIndex(funcDefinitionAst.funcName, marineILs.Count);

            bool retFlag = false;
            var stackAllockIndex = marineILs.Count;
            marineILs.Add(new NoOpIL());
            foreach (var statementAst in funcDefinitionAst.statementAsts)
                retFlag = retFlag || StatementILGenerate(statementAst, funcDefinitionAst.args.Length, variables, null);
            if (funcDefinitionAst.statementAsts.Length == 0)
                marineILs.Add(new NoOpIL());
            if (retFlag == false)
            {
                marineILs.Add(new PushValueIL(new UnitType()));
                marineILs.Add(new RetIL(funcDefinitionAst.args.Length));
            }

            if (variables.MaxLocalVariableCount != 0)
                marineILs[stackAllockIndex] = new StackAllocIL(variables.MaxLocalVariableCount);
        }

        void ActionFuncILGenerate(ActionFuncData actionFuncData, string[] globalVariableNames)
        {
            var args = new[] {VariableAst.Create(new Token(default, "_action", default))}
                .Concat(actionFuncData.actionAst.args).ToArray();
            var variables = new FuncScopeVariables(args, globalVariableNames, actionFuncData.captureVarNames);
            var funcDefinitionAst = FuncDefinitionAst.Create(
                null,
                actionFuncData.funcName,
                args,
                actionFuncData.actionAst.statementAsts,
                null
            );
            FuncDefinitionILGenerate(funcDefinitionAst, variables);
        }

        bool StatementILGenerate(StatementAst statementAst, int argCount, FuncScopeVariables variables,
            BreakIndex breakIndex)
        {
            if (statementAst.GetReturnAst() != null)
            {
                ReturnILGenerate(statementAst.GetReturnAst(), argCount, variables, breakIndex);
                return true;
            }
            else if (statementAst.GetExprStatementAst() != null)
            {
                ExprILGenerate(statementAst.GetExprStatementAst().expr, argCount, variables, breakIndex);
                marineILs.Add(new PopIL());
            }
            else if (statementAst.GetReAssignmentVariableAst() != null)
                ReAssignmentVariableILGenerate(statementAst.GetReAssignmentVariableAst(), argCount, variables,
                    breakIndex);
            else if (statementAst.GetReAssignmentIndexerAst() != null)
                ReAssignmentIndexerILGenerate(statementAst.GetReAssignmentIndexerAst(), argCount, variables,
                    breakIndex);
            else if (statementAst.GetAssignmentVariableAst() != null)
                AssignmentILGenerate(statementAst.GetAssignmentVariableAst(), argCount, variables, breakIndex);
            else if (statementAst.GetInstanceFieldAssignmentAst() != null)
                InstanceFieldAssignmentILGenerate(statementAst.GetInstanceFieldAssignmentAst(), argCount, variables, breakIndex);
            else if (statementAst.GetStaticFieldAssignmentAst() != null)
                StaticFieldAssignmentILGenerate(statementAst.GetStaticFieldAssignmentAst(), argCount, variables, breakIndex);
            else if (statementAst.GetWhileAst() != null)
                WhileILGenerate(statementAst.GetWhileAst(), argCount, variables);
            else if (statementAst.GetForAst() != null)
                ForILGenerate(statementAst.GetForAst(), argCount, variables);
            else if (statementAst.GetForEachAst() != null)
                ForEachILGenerate(statementAst.GetForEachAst(), argCount, variables);
            else if (statementAst.GetYieldAst() != null)
                marineILs.Add(new YieldIL());
            else if (statementAst.GetBreakAst() != null)
                marineILs.Add(new BreakIL(breakIndex));

            return false;
        }

        void ReturnILGenerate(ReturnAst returnAst, int argCount, FuncScopeVariables variables, BreakIndex breakIndex)
        {
            ExprILGenerate(returnAst.expr, argCount, variables, breakIndex);
            marineILs.Add(new RetIL(argCount));
        }

        void ExprILGenerate(ExprAst exprAst, int argCount, FuncScopeVariables variables, BreakIndex breakIndex)
        {
            if (exprAst.GetFuncCallAst() != null)
                FuncCallILGenerate(exprAst.GetFuncCallAst(), argCount, variables, breakIndex);
            else if (exprAst.GetBinaryOpAst() != null)
                BinaryOpILGenerate(exprAst.GetBinaryOpAst(), argCount, variables, breakIndex);
            else if (exprAst.GetVariableAst() != null)
                VariableILGenerate(exprAst.GetVariableAst(), argCount, variables, breakIndex);
            else if (exprAst.GetValueAst() != null)
                ValueILGenerate(exprAst.GetValueAst());
            else if (exprAst.GetIfExprAst() != null)
                IfILGenerate(exprAst.GetIfExprAst(), argCount, variables, breakIndex);
            else if (exprAst.GetInstanceFuncCallAst() != null)
                InstanceFuncCallILGenerate(exprAst.GetInstanceFuncCallAst(), argCount, variables, breakIndex);
            else if (exprAst.GetStaticFuncCallAst() != null)
                StaticFuncCallILGenerate(exprAst.GetStaticFuncCallAst(), argCount, variables, breakIndex);
            else if (exprAst.GetInstanceFieldAst() != null)
                InstanceFieldILGenerate(exprAst.GetInstanceFieldAst(), argCount, variables, breakIndex);
            else if (exprAst.GetStaticFieldAst() != null)
                StaticFieldILGenerate(exprAst.GetStaticFieldAst());
            else if (exprAst.GetGetIndexerAst() != null)
                GetGetIndexerILGenerate(exprAst.GetGetIndexerAst(), argCount, variables, breakIndex);
            else if (exprAst.GetArrayLiteralAst() != null)
                ArrayLiteralILGenerate(exprAst.GetArrayLiteralAst(), argCount, variables, breakIndex);
            else if (exprAst.GetActionAst() != null)
                ActionILGenerate(exprAst.GetActionAst(), argCount, variables, breakIndex);
            else if (exprAst.GetAwaitAst() != null)
                AwaitILGenerate(exprAst.GetAwaitAst(), argCount, variables, breakIndex);
            else if (exprAst.GetUnaryOpAst() != null)
                UnaryOpILGenerate(exprAst.GetUnaryOpAst(), argCount, variables, breakIndex);
        }

        void FuncCallILGenerate(FuncCallAst funcCallAst, int argCount, FuncScopeVariables args, BreakIndex breakIndex)
        {
            foreach (var exprAst in funcCallAst.args)
                ExprILGenerate(exprAst, argCount, args, breakIndex);

            if (namespaceTable.ContainFunc(funcCallAst.FuncName))
                marineILs.Add(
                    new MarineFuncCallIL(
                        funcCallAst.FuncName,
                        namespaceTable.GetFuncIlIndex(funcCallAst.FuncName), 
                        funcCallAst.args.Length
                    )
                );
            else
                marineILs.Add(new CSharpFuncCallIL(csharpFuncDict[NameUtil.GetUpperCamelName(funcCallAst.FuncName)],
                    funcCallAst.args.Length));
        }

        void BinaryOpILGenerate(BinaryOpAst binaryOpAst, int argCount, FuncScopeVariables args, BreakIndex breakIndex)
        {
            ExprILGenerate(binaryOpAst.leftExpr, argCount, args, breakIndex);
            ExprILGenerate(binaryOpAst.rightExpr, argCount, args, breakIndex);
            marineILs.Add(new BinaryOpIL(binaryOpAst.opKind));
        }

        void VariableILGenerate(VariableAst variableAst, int argCount, FuncScopeVariables variables,
            BreakIndex breakIndex)
        {
            var captureIdx = variables.GetCaptureVariableIdx(variableAst.VarName);
            if (captureIdx.HasValue)
            {
                var ast =
                    InstanceFuncCallAst.Create(
                        VariableAst.Create(new Token(default, "_action", default)),
                        FuncCallAst.Create(
                            new Token(default, "get", default),
                            new[] {ValueAst.Create(captureIdx.Value, default)},
                            new Token(default, "")
                        )
                    );
                ExprILGenerate(ast, argCount, variables, breakIndex);
            }
            else
                marineILs.Add(new LoadIL(variables.GetVariableIdx(variableAst.VarName)));
        }

        void IfILGenerate(IfExprAst ifExprAst, int argCount, FuncScopeVariables variables, BreakIndex breakIndex)
        {
            ExprILGenerate(ifExprAst.conditionExpr, argCount, variables, breakIndex);
            var jumpFalseInsertIndex = marineILs.Count;
            marineILs.Add(null);
            BlockExprGenerate(ifExprAst.thenStatements, argCount, variables, breakIndex);
            var jumpInsertIndex = marineILs.Count;
            marineILs.Add(null);
            marineILs[jumpFalseInsertIndex] = new JumpFalseIL(marineILs.Count);
            if (ifExprAst.elseStatements.Length == 0)
                marineILs.Add(new PushValueIL(new UnitType()));
            else
                BlockExprGenerate(ifExprAst.elseStatements, argCount, variables, breakIndex);
            marineILs[jumpInsertIndex] = new JumpIL(marineILs.Count);
        }

        void BlockExprGenerate(StatementAst[] statementAsts, int argCount, FuncScopeVariables variables,
            BreakIndex breakIndex)
        {
            variables.InScope();

            for (var i = 0; i < statementAsts.Length - 1; i++)
                StatementILGenerate(statementAsts[i], argCount, variables, breakIndex);

            if (statementAsts.Length > 0)
            {
                var lastStatementAst = statementAsts.Last();
                if (lastStatementAst.GetExprStatementAst() != null)
                {
                    ExprILGenerate(lastStatementAst.GetExprStatementAst().expr, argCount, variables, breakIndex);
                    variables.OutScope();
                    return;
                }

                StatementILGenerate(lastStatementAst, argCount, variables, breakIndex);
            }

            marineILs.Add(new PushValueIL(new UnitType()));
            variables.OutScope();
        }

        void InstanceFuncCallILGenerate(InstanceFuncCallAst instanceFuncCallAst, int argCount,
            FuncScopeVariables variables, BreakIndex breakIndex)
        {

            ExprILGenerate(instanceFuncCallAst.instanceExpr, argCount, variables, breakIndex);
            var funcCallAst = instanceFuncCallAst.instancefuncCallAst;
            var csharpFuncName = NameUtil.GetUpperCamelName(funcCallAst.FuncName);

            foreach (var arg in funcCallAst.args)
                ExprILGenerate(arg, argCount, variables, breakIndex);
            marineILs.Add(
                new InstanceCSharpFuncCallIL(csharpFuncName, funcCallAst.args.Length,
                    new ILDebugInfo(funcCallAst.Range.Start))
            );
        }

        void StaticFuncCallILGenerate(StaticFuncCallAst staticFuncCallAst, int argCount,
            FuncScopeVariables variables, BreakIndex breakIndex)
        {

            var funcCallAst = staticFuncCallAst.funcCallAst;
            var csharpFuncName = NameUtil.GetUpperCamelName(funcCallAst.FuncName);
            foreach (var arg in funcCallAst.args)
                ExprILGenerate(arg, argCount, variables, breakIndex);

            var type = staticTypeDict[staticFuncCallAst.ClassName];
            var methodBases =
                csharpFuncName == "New" ?
                    type.GetConstructors().Cast<MethodBase>() :
                    type.GetMethods(BindingFlags.Public | BindingFlags.Static).Where(e => e.Name == csharpFuncName);

            marineILs.Add(
                new StaticCSharpFuncCallIL(type, methodBases.ToArray(), csharpFuncName,
                    funcCallAst.args.Length, new ILDebugInfo(funcCallAst.Range.Start))
            );
        }

        void InstanceFieldILGenerate(InstanceFieldAst instanceFieldAst, int argCount, FuncScopeVariables variables,
            BreakIndex breakIndex)
        {
            ExprILGenerate(instanceFieldAst.instanceExpr, argCount, variables, breakIndex);
            marineILs.Add(
                new InstanceCSharpFieldLoadIL(
                    instanceFieldAst.variableAst.VarName,
                    new ILDebugInfo(instanceFieldAst.variableAst.Range.Start)
                )
            );
        }

        void StaticFieldILGenerate(StaticFieldAst staticFieldAst)
        {
            var type = staticTypeDict[staticFieldAst.ClassName];
            if (type.IsEnum)
            {
                marineILs.Add(
                    new PushValueIL(
                        Enum.Parse(type, NameUtil.GetUpperCamelName(staticFieldAst.variableAst.VarName))
                    )
                );
            }
            else
            {
                marineILs.Add(
                    new StaticCSharpFieldLoadIL(
                        type,
                        staticFieldAst.variableAst.VarName, 
                        new ILDebugInfo(staticFieldAst.Range.Start)
                    )
                );
            }
        }

        void GetGetIndexerILGenerate(GetIndexerAst getIndexerAst, int argCount, FuncScopeVariables variables,
            BreakIndex breakIndex)
        {
            ExprILGenerate(getIndexerAst.instanceExpr, argCount, variables, breakIndex);
            ExprILGenerate(getIndexerAst.indexExpr, argCount, variables, breakIndex);
            marineILs.Add(
                new InstanceCSharpIndexerLoadIL(new ILDebugInfo(getIndexerAst.instanceExpr.Range.End))
            );
        }

        void ArrayLiteralILGenerate(ArrayLiteralAst arrayLiteralAst, int argCount, FuncScopeVariables variables,
            BreakIndex breakIndex)
        {
            foreach (var exprAst in arrayLiteralAst.arrayLiteralExprs.exprAsts)
                ExprILGenerate(exprAst, argCount, variables, breakIndex);
            marineILs.Add(new CreateArrayIL(
                arrayLiteralAst.arrayLiteralExprs.exprAsts.Length,
                arrayLiteralAst.arrayLiteralExprs.size
            ));
        }

        void ActionILGenerate(ActionAst actionAst, int argCount, FuncScopeVariables variables, BreakIndex breakIndex)
        {
            var captures =
                actionAst.statementAsts.SelectMany(x => x.LookUp<VariableAst>())
                    .Where(x => variables.ExistVariable(x.VarName))
                    .Distinct(new VariableAst.Comparer())
                    .ToArray();

            var actionFuncName = "_action_func<" + actionFuncDataList.Count + ">";
            actionFuncDataList.Add(new ActionFuncData(actionFuncName, actionAst,
                captures.Select(x => x.VarName).ToArray()));

            var ast =
                InstanceFuncCallAst.Create(
                    VariableAst.Create(new Token(default, "action_object_generator", default)),
                    FuncCallAst.Create(
                        new Token(default, "generate", default),
                        new ExprAst[]
                        {
                            ValueAst.Create(actionFuncName, default),
                            ArrayLiteralAst.Create(
                                default,
                                ArrayLiteralAst.ArrayLiteralExprs.Create(captures, captures.Length),
                                default
                            )
                        },
                        new Token(default, "")
                    )
                );
            ExprILGenerate(ast, argCount, variables, breakIndex);
        }

        void AwaitILGenerate(AwaitAst awaitAst, int argCount, FuncScopeVariables variables, BreakIndex breakIndex)
        {
            ExprILGenerate(awaitAst.instanceExpr, argCount, variables, breakIndex);
            var iterVariable = variables.CreateUnnamedLocalVariableIdx();
            var resultVariable = variables.CreateUnnamedLocalVariableIdx();
            marineILs.Add(new StoreIL(iterVariable));
            var jumpIndex = marineILs.Count;
            marineILs.Add(new LoadIL(iterVariable));
            marineILs.Add(new MoveNextIL());
            marineILs.Add(new JumpFalseIL(jumpIndex + 8));
            marineILs.Add(new LoadIL(iterVariable));
            marineILs.Add(new GetIterCurrentL());
            marineILs.Add(new StoreIL(resultVariable));
            marineILs.Add(new YieldIL());
            marineILs.Add(new JumpIL(jumpIndex));
            marineILs.Add(new LoadIL(resultVariable));
        }

        void UnaryOpILGenerate(UnaryOpAst unaryOpAst, int argCount, FuncScopeVariables variables, BreakIndex breakIndex)
        {
            ExprILGenerate(unaryOpAst.expr, argCount, variables, breakIndex);
            marineILs.Add(new UnaryOpIL(unaryOpAst.opToken.tokenType));
        }

        void ReAssignmentVariableILGenerate(ReAssignmentVariableAst reAssignmentAst, int argCount,
            FuncScopeVariables variables, BreakIndex breakIndex)
        {
            var captureIdx = variables.GetCaptureVariableIdx(reAssignmentAst.variableAst.VarName);
            if (captureIdx.HasValue)
            {
                var ast =
                    InstanceFuncCallAst.Create(
                        VariableAst.Create(new Token(default, "_action", default)),
                        FuncCallAst.Create(
                            new Token(default, "set", default),
                            new[] {ValueAst.Create(captureIdx.Value, default), reAssignmentAst.expr},
                            new Token(default, "")
                        )
                    );
                ExprILGenerate(ast, argCount, variables, breakIndex);
            }
            else
            {
                ExprILGenerate(reAssignmentAst.expr, argCount, variables, breakIndex);
                marineILs.Add(new StoreIL(variables.GetVariableIdx(reAssignmentAst.variableAst.VarName)));
            }
        }

        void ReAssignmentIndexerILGenerate(ReAssignmentIndexerAst reAssignmentAst, int argCount,
            FuncScopeVariables variables, BreakIndex breakIndex)
        {
            ExprILGenerate(reAssignmentAst.getIndexerAst.instanceExpr, argCount, variables, breakIndex);
            ExprILGenerate(reAssignmentAst.getIndexerAst.indexExpr, argCount, variables, breakIndex);
            ExprILGenerate(reAssignmentAst.assignmentExpr, argCount, variables, breakIndex);
            marineILs.Add(
                new InstanceCSharpIndexerStoreIL(
                    new ILDebugInfo(reAssignmentAst.getIndexerAst.instanceExpr.Range.End)));
        }

        void AssignmentILGenerate(AssignmentVariableAst assignmentAst, int argCount, FuncScopeVariables variables,
            BreakIndex breakIndex)
        {
            ExprILGenerate(assignmentAst.expr, argCount, variables, breakIndex);
            variables.AddLocalVariable(assignmentAst.variableAst.VarName);
            marineILs.Add(new StoreIL(variables.GetVariableIdx(assignmentAst.variableAst.VarName)));
        }

        void InstanceFieldAssignmentILGenerate(InstanceFieldAssignmentAst fieldAssignmentAst, int argCount,
            FuncScopeVariables variables, BreakIndex breakIndex)
        {

            ExprILGenerate(fieldAssignmentAst.instanceFieldAst.instanceExpr, argCount, variables, breakIndex);
            ExprILGenerate(fieldAssignmentAst.expr, argCount, variables, breakIndex);
            marineILs.Add(
                new InstanceCSharpFieldStoreIL(
                    fieldAssignmentAst.instanceFieldAst.variableAst.VarName,
                    new ILDebugInfo(fieldAssignmentAst.instanceFieldAst.variableAst.Range.Start)
                )
            );
        }

        void StaticFieldAssignmentILGenerate(StaticFieldAssignmentAst staticFieldAssignmentAst, int argCount,
            FuncScopeVariables variables, BreakIndex breakIndex)
        {
            ExprILGenerate(staticFieldAssignmentAst.expr, argCount, variables, breakIndex);
            marineILs.Add(
                new StaticCSharpFieldStoreIL(staticTypeDict[staticFieldAssignmentAst.staticFieldAst.ClassName],
                    staticFieldAssignmentAst.staticFieldAst.variableAst.VarName,
                    new ILDebugInfo(staticFieldAssignmentAst.Range.Start)
                )
            );
        }

        void WhileILGenerate(WhileAst whileAst, int argCount, FuncScopeVariables variables)
        {
            variables.InScope();
            var breakIndex = new BreakIndex();

            var jumpIL = new JumpIL(marineILs.Count);
            ExprILGenerate(whileAst.conditionExpr, argCount, variables, breakIndex);
            var jumpFalseILInsertIndex = marineILs.Count;
            marineILs.Add(null);
            foreach (var statementAst in whileAst.statements)
                StatementILGenerate(statementAst, argCount, variables, breakIndex);
            marineILs.Add(jumpIL);
            marineILs[jumpFalseILInsertIndex] = new JumpFalseIL(marineILs.Count);
            breakIndex.Index = marineILs.Count;
            variables.OutScope();
        }

        void ForILGenerate(ForAst forAst, int argCount, FuncScopeVariables variables)
        {
            variables.InScope();
            variables.AddLocalVariable(forAst.initVariable.VarName);
            var breakIndex = new BreakIndex();
            var countVarIdx = variables.GetVariableIdx(forAst.initVariable.VarName);
            var maxVarIdx = variables.CreateUnnamedLocalVariableIdx();
            var addVarIdx = variables.CreateUnnamedLocalVariableIdx();
            ExprILGenerate(forAst.initExpr, argCount, variables, breakIndex);
            marineILs.Add(new StoreIL(countVarIdx));
            ExprILGenerate(forAst.maxValueExpr, argCount, variables, breakIndex);
            marineILs.Add(new StoreIL(maxVarIdx));
            ExprILGenerate(forAst.addValueExpr, argCount, variables, breakIndex);
            marineILs.Add(new StoreIL(addVarIdx));
            var jumpIL = new JumpIL(marineILs.Count);
            marineILs.Add(new LoadIL(countVarIdx));
            marineILs.Add(new LoadIL(maxVarIdx));
            marineILs.Add(new BinaryOpIL(TokenType.LessEqualOp));
            var jumpFalseILInsertIndex = marineILs.Count;
            marineILs.Add(null);
            foreach (var statementAst in forAst.statements)
                StatementILGenerate(statementAst, argCount, variables, breakIndex);
            marineILs.Add(new LoadIL(countVarIdx));
            marineILs.Add(new LoadIL(addVarIdx));
            marineILs.Add(new BinaryOpIL(TokenType.PlusOp));
            marineILs.Add(new StoreIL(countVarIdx));
            marineILs.Add(jumpIL);
            marineILs[jumpFalseILInsertIndex] = new JumpFalseIL(marineILs.Count);
            breakIndex.Index = marineILs.Count;
            variables.OutScope();
        }

        void ForEachILGenerate(ForEachAst forEachAst, int argCount, FuncScopeVariables variables)
        {
            variables.InScope();
            variables.AddLocalVariable(forEachAst.variable.VarName);

            var breakIndex = new BreakIndex();
            var currentVarIdx = variables.GetVariableIdx(forEachAst.variable.VarName);
            var iterVarIdx = variables.CreateUnnamedLocalVariableIdx();
            ExprILGenerate(forEachAst.expr, argCount, variables, breakIndex);
            marineILs.Add(new InstanceCSharpFuncCallIL("GetEnumerator", 0));
            marineILs.Add(new StoreIL(iterVarIdx));
            var jumpIL = new JumpIL(marineILs.Count);
            marineILs.Add(new LoadIL(iterVarIdx));
            marineILs.Add(new InstanceCSharpFuncCallIL("MoveNext", 0));
            var jumpFalseILInsertIndex = marineILs.Count;
            marineILs.Add(null);
            marineILs.Add(new LoadIL(iterVarIdx));
            marineILs.Add(new InstanceCSharpFieldLoadIL("current"));
            marineILs.Add(new StoreIL(currentVarIdx));
            foreach (var statementAst in forEachAst.statements)
                StatementILGenerate(statementAst, argCount, variables, breakIndex);
            marineILs.Add(jumpIL);
            marineILs[jumpFalseILInsertIndex] = new JumpFalseIL(marineILs.Count);
            breakIndex.Index = marineILs.Count;
            variables.OutScope();
        }

        void ValueILGenerate(ValueAst valueAst)
        {
            marineILs.Add(new PushValueIL(valueAst.value));
        }
    }
}