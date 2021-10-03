using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MarineLang.Models;
using MarineLang.Models.Asts;
using MarineLang.Utils;
using MarineLang.VirtualMachines.MarineILs;
using MineUtil;

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
        private struct GenerateArgs
        {
            public int argCount;
            public FuncScopeVariables variables;
            public BreakIndex breakIndex;
            public readonly NamespaceTable globalNamespaceTable;
            public NamespaceTable namespaceTable;

            public GenerateArgs(
                int argCount,
                FuncScopeVariables variables,
                BreakIndex breakIndex,
                NamespaceTable globalNamespaceTable,
                NamespaceTable namespaceTable
            )
            {
                this.argCount = argCount;
                this.variables = variables;
                this.breakIndex = breakIndex;
                this.globalNamespaceTable = globalNamespaceTable;
                this.namespaceTable = namespaceTable;
            }
        }

        IReadOnlyDictionary<string, MethodInfo> csharpFuncDict;
        IReadOnlyDictionary<string, Type> staticTypeDict;
        readonly List<IMarineIL> marineILs = new List<IMarineIL>();
        readonly List<ActionFuncData> actionFuncDataList = new List<ActionFuncData>();
        readonly IEnumerable<MarineProgramUnit> marineProgramUnits;

        public ILGenerator(IEnumerable<MarineProgramUnit> marineProgramUnits)
        {
            this.marineProgramUnits = marineProgramUnits;
        }

        public ILGeneratedData Generate(
            IReadOnlyDictionary<string, MethodInfo> csharpFuncDict,
            IReadOnlyDictionary<string, Type> staticTypeDict,
            string[] globalVariableNames
            )
        {
            this.csharpFuncDict = csharpFuncDict;
            this.staticTypeDict = staticTypeDict;
            var namespaceTable = new NamespaceTable();

            foreach (var marineProgramUnit in marineProgramUnits)
            {
                var funcNames = marineProgramUnit.programAst.funcDefinitionAsts.Select(x => x.funcName);
                namespaceTable.AddFuncILIndex(marineProgramUnit.namespaceStrings, funcNames);
            }

            foreach (var marineProgramUnit in marineProgramUnits)
            {
                ProgramILGenerate(
                    namespaceTable,
                    namespaceTable.GetChildNamespace(marineProgramUnit.namespaceStrings),
                    marineProgramUnit.programAst,
                    globalVariableNames
                );
            }
            return new ILGeneratedData(namespaceTable, marineILs);
        }

        void ProgramILGenerate(
            NamespaceTable globalNamespaceTable,
            NamespaceTable namespaceTable,
            ProgramAst programAst,
            string[] globalVariableNames
        )
        {
            foreach (var funcDefinitionAst in programAst.funcDefinitionAsts)
                FuncDefinitionILGenerate(
                    globalNamespaceTable,
                    namespaceTable,
                    funcDefinitionAst,
                    new FuncScopeVariables(funcDefinitionAst.args, globalVariableNames)
                );
            for (var i = 0; i < actionFuncDataList.Count; i++)
                ActionFuncILGenerate(globalNamespaceTable, namespaceTable, actionFuncDataList[i], globalVariableNames);
        }

        void FuncDefinitionILGenerate(
            NamespaceTable globalNamespaceTable,
            NamespaceTable namespaceTable,
            FuncDefinitionAst funcDefinitionAst,
            FuncScopeVariables variables
        )
        {
            namespaceTable.SetFuncILIndex(funcDefinitionAst.funcName, marineILs.Count);

            bool retFlag = false;
            var stackAllockIndex = marineILs.Count;
            marineILs.Add(new NoOpIL());

            foreach (var statementAst in funcDefinitionAst.statementAsts)
                retFlag =
                    retFlag ||
                    StatementILGenerate(
                        statementAst,
                        new GenerateArgs(funcDefinitionAst.args.Length, variables, null, globalNamespaceTable, namespaceTable)
                    );

            if (funcDefinitionAst.statementAsts.Length == 0)
                marineILs.Add(new NoOpIL());
            if (retFlag == false)
            {
                marineILs.Add(new PushValueIL(Unit.Value));
                marineILs.Add(new RetIL(funcDefinitionAst.args.Length));
            }

            if (variables.MaxLocalVariableCount != 0)
                marineILs[stackAllockIndex] = new StackAllocIL(variables.MaxLocalVariableCount);
        }

        void ActionFuncILGenerate(NamespaceTable globalNamespaceTable, NamespaceTable namespaceTable, ActionFuncData actionFuncData, string[] globalVariableNames)
        {
            var args = new[] { VariableAst.Create(new Token(default, "_action", default)) }
                .Concat(actionFuncData.actionAst.args).ToArray();
            var variables = new FuncScopeVariables(args, globalVariableNames, actionFuncData.captureVarNames);
            var funcDefinitionAst = FuncDefinitionAst.Create(
                null,
                actionFuncData.funcName,
                args,
                actionFuncData.actionAst.statementAsts,
                null
            );
            FuncDefinitionILGenerate(globalNamespaceTable, namespaceTable, funcDefinitionAst, variables);
        }

        bool StatementILGenerate(StatementAst statementAst, GenerateArgs generateArgs)
        {
            if (statementAst.GetReturnAst() != null)
            {
                ReturnILGenerate(statementAst.GetReturnAst(), generateArgs);
                return true;
            }
            else if (statementAst.GetExprStatementAst() != null)
            {
                ExprILGenerate(statementAst.GetExprStatementAst().expr, generateArgs);
                marineILs.Add(new PopIL());
            }
            else if (statementAst.GetReAssignmentVariableAst() != null)
                ReAssignmentVariableILGenerate(statementAst.GetReAssignmentVariableAst(), generateArgs);
            else if (statementAst.GetReAssignmentIndexerAst() != null)
                ReAssignmentIndexerILGenerate(statementAst.GetReAssignmentIndexerAst(), generateArgs);
            else if (statementAst.GetAssignmentVariableAst() != null)
                AssignmentILGenerate(statementAst.GetAssignmentVariableAst(), generateArgs);
            else if (statementAst.GetInstanceFieldAssignmentAst() != null)
                InstanceFieldAssignmentILGenerate(statementAst.GetInstanceFieldAssignmentAst(), generateArgs);
            else if (statementAst.GetStaticFieldAssignmentAst() != null)
                StaticFieldAssignmentILGenerate(statementAst.GetStaticFieldAssignmentAst(), generateArgs);
            else if (statementAst.GetWhileAst() != null)
                WhileILGenerate(statementAst.GetWhileAst(), generateArgs);
            else if (statementAst.GetForAst() != null)
                ForILGenerate(statementAst.GetForAst(), generateArgs);
            else if (statementAst.GetForEachAst() != null)
                ForEachILGenerate(statementAst.GetForEachAst(), generateArgs);
            else if (statementAst.GetYieldAst() != null)
                marineILs.Add(new YieldIL());
            else if (statementAst.GetBreakAst() != null)
                marineILs.Add(new BreakIL(generateArgs.breakIndex));

            return false;
        }

        void ReturnILGenerate(ReturnAst returnAst, GenerateArgs generateArgs)
        {
            ExprILGenerate(returnAst.expr, generateArgs);
            marineILs.Add(new RetIL(generateArgs.argCount));
        }

        void ExprILGenerate(ExprAst exprAst, GenerateArgs generateArgs)
        {
            if (exprAst.GetFuncCallAst() != null)
                FuncCallILGenerate(exprAst.GetFuncCallAst(), generateArgs);
            else if (exprAst.GetBinaryOpAst() != null)
                BinaryOpILGenerate(exprAst.GetBinaryOpAst(), generateArgs);
            else if (exprAst.GetVariableAst() != null)
                VariableILGenerate(exprAst.GetVariableAst(), generateArgs);
            else if (exprAst.GetValueAst() != null)
                ValueILGenerate(exprAst.GetValueAst());
            else if (exprAst.GetIfExprAst() != null)
                IfILGenerate(exprAst.GetIfExprAst(), generateArgs);
            else if (exprAst.GetInstanceFuncCallAst() != null)
                InstanceFuncCallILGenerate(exprAst.GetInstanceFuncCallAst(), generateArgs);
            else if (exprAst.GetStaticFuncCallAst() != null)
                StaticFuncCallILGenerate(exprAst.GetStaticFuncCallAst(), generateArgs);
            else if (exprAst.GetInstanceFieldAst() != null)
                InstanceFieldILGenerate(exprAst.GetInstanceFieldAst(), generateArgs);
            else if (exprAst.GetStaticFieldAst() != null)
                StaticFieldILGenerate(exprAst.GetStaticFieldAst());
            else if (exprAst.GetGetIndexerAst() != null)
                GetGetIndexerILGenerate(exprAst.GetGetIndexerAst(), generateArgs);
            else if (exprAst.GetArrayLiteralAst() != null)
                ArrayLiteralILGenerate(exprAst.GetArrayLiteralAst(), generateArgs);
            else if (exprAst.GetActionAst() != null)
                ActionILGenerate(exprAst.GetActionAst(), generateArgs);
            else if (exprAst.GetAwaitAst() != null)
                AwaitILGenerate(exprAst.GetAwaitAst(), generateArgs);
            else if (exprAst.GetUnaryOpAst() != null)
                UnaryOpILGenerate(exprAst.GetUnaryOpAst(), generateArgs);
            else
            {
                throw new Exception("IL生成に失敗");
            }
        }

        void FuncCallILGenerate(FuncCallAst funcCallAst, GenerateArgs generateArgs)
        {
            foreach (var exprAst in funcCallAst.args)
                ExprILGenerate(exprAst, generateArgs);

            if (funcCallAst.namespaceTokens.Any())
            {
                var funcILIndex = generateArgs.globalNamespaceTable.AddFuncILIndex(funcCallAst.NamespaceSettings, funcCallAst.FuncName);
                marineILs.Add(
                    new MarineFuncCallIL(
                        funcCallAst.FuncName,
                        funcILIndex,
                        funcCallAst.args.Length
                    )
                );
                return;
            }
            
            if (generateArgs.namespaceTable.ContainFunc(funcCallAst.FuncName))
                marineILs.Add(
                    new MarineFuncCallIL(
                        funcCallAst.FuncName,
                        generateArgs.namespaceTable.GetFuncILIndex(funcCallAst.FuncName),
                        funcCallAst.args.Length
                    )
                );
            else
                marineILs.Add(new CSharpFuncCallIL(csharpFuncDict[NameUtil.GetUpperCamelName(funcCallAst.FuncName)],
                    funcCallAst.args.Length));
        }

        void BinaryOpILGenerate(BinaryOpAst binaryOpAst, GenerateArgs generateArgs)
        {
            ExprILGenerate(binaryOpAst.leftExpr, generateArgs);
            ExprILGenerate(binaryOpAst.rightExpr, generateArgs);
            marineILs.Add(new BinaryOpIL(binaryOpAst.opKind));
        }

        void VariableILGenerate(VariableAst variableAst, GenerateArgs generateArgs)
        {
            var captureIdx = generateArgs.variables.GetCaptureVariableIdx(variableAst.VarName);
            if (captureIdx.HasValue)
            {
                var ast =
                    InstanceFuncCallAst.Create(
                        VariableAst.Create(new Token(default, "_action", default)),
                        FuncCallAst.Create(
                            new Token(default, "get", default),
                            new[] { ValueAst.Create(captureIdx.Value, default) },
                            new Token(default, "")
                        )
                    );
                ExprILGenerate(ast, generateArgs);
            }
            else
                marineILs.Add(new LoadIL(generateArgs.variables.GetVariableIdx(variableAst.VarName)));
        }

        void IfILGenerate(IfExprAst ifExprAst, GenerateArgs generateArgs)
        {
            ExprILGenerate(ifExprAst.conditionExpr, generateArgs);
            var jumpFalseInsertIndex = marineILs.Count;
            marineILs.Add(null);
            BlockExprGenerate(ifExprAst.thenStatements, generateArgs);
            var jumpInsertIndex = marineILs.Count;
            marineILs.Add(null);
            marineILs[jumpFalseInsertIndex] = new JumpFalseIL(marineILs.Count);
            if (ifExprAst.elseStatements.Length == 0)
                marineILs.Add(new PushValueIL(Unit.Value));
            else
                BlockExprGenerate(ifExprAst.elseStatements, generateArgs);
            marineILs[jumpInsertIndex] = new JumpIL(marineILs.Count);
        }

        void BlockExprGenerate(StatementAst[] statementAsts, GenerateArgs generateArgs)
        {
            generateArgs.variables.InScope();

            for (var i = 0; i < statementAsts.Length - 1; i++)
                StatementILGenerate(statementAsts[i], generateArgs);

            if (statementAsts.Length > 0)
            {
                var lastStatementAst = statementAsts.Last();
                if (lastStatementAst.GetExprStatementAst() != null)
                {
                    ExprILGenerate(lastStatementAst.GetExprStatementAst().expr, generateArgs);
                    generateArgs.variables.OutScope();
                    return;
                }

                StatementILGenerate(lastStatementAst, generateArgs);
            }

            marineILs.Add(new PushValueIL(Unit.Value));
            generateArgs.variables.OutScope();
        }

        void InstanceFuncCallILGenerate(InstanceFuncCallAst instanceFuncCallAst, GenerateArgs generateArgs)
        {
            ExprILGenerate(instanceFuncCallAst.instanceExpr, generateArgs);
            var funcCallAst = instanceFuncCallAst.instancefuncCallAst;
            var csharpFuncName = NameUtil.GetUpperCamelName(funcCallAst.FuncName);

            foreach (var arg in funcCallAst.args)
                ExprILGenerate(arg, generateArgs);
            marineILs.Add(
                new InstanceCSharpFuncCallIL(csharpFuncName, funcCallAst.args.Length,
                    new ILDebugInfo(funcCallAst.Range.Start))
            );
        }

        void StaticFuncCallILGenerate(StaticFuncCallAst staticFuncCallAst, GenerateArgs generateArgs)
        {

            var funcCallAst = staticFuncCallAst.funcCallAst;
            var csharpFuncName = NameUtil.GetUpperCamelName(funcCallAst.FuncName);
            foreach (var arg in funcCallAst.args)
                ExprILGenerate(arg, generateArgs);

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

        void InstanceFieldILGenerate(InstanceFieldAst instanceFieldAst, GenerateArgs generateArgs)
        {
            ExprILGenerate(instanceFieldAst.instanceExpr, generateArgs);
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

        void GetGetIndexerILGenerate(GetIndexerAst getIndexerAst, GenerateArgs generateArgs)
        {
            ExprILGenerate(getIndexerAst.instanceExpr, generateArgs);
            ExprILGenerate(getIndexerAst.indexExpr, generateArgs);
            marineILs.Add(
                new InstanceCSharpIndexerLoadIL(new ILDebugInfo(getIndexerAst.instanceExpr.Range.End))
            );
        }

        void ArrayLiteralILGenerate(ArrayLiteralAst arrayLiteralAst, GenerateArgs generateArgs)
        {
            foreach (var exprAst in arrayLiteralAst.arrayLiteralExprs.exprAsts)
                ExprILGenerate(exprAst, generateArgs);
            marineILs.Add(new CreateArrayIL(
                arrayLiteralAst.arrayLiteralExprs.exprAsts.Length,
                arrayLiteralAst.arrayLiteralExprs.size
            ));
        }

        void ActionILGenerate(ActionAst actionAst, GenerateArgs generateArgs)
        {
            var captures =
                actionAst.statementAsts.SelectMany(x => x.LookUp<VariableAst>())
                    .Where(x => generateArgs.variables.ExistVariable(x.VarName))
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
            ExprILGenerate(ast, generateArgs);
        }

        void AwaitILGenerate(AwaitAst awaitAst, GenerateArgs generateArgs)
        {
            ExprILGenerate(awaitAst.instanceExpr, generateArgs);
            var iterVariable = generateArgs.variables.CreateUnnamedLocalVariableIdx();
            var resultVariable = generateArgs.variables.CreateUnnamedLocalVariableIdx();
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

        void UnaryOpILGenerate(UnaryOpAst unaryOpAst, GenerateArgs generateArgs)
        {
            ExprILGenerate(unaryOpAst.expr, generateArgs);
            marineILs.Add(new UnaryOpIL(unaryOpAst.opToken.tokenType));
        }

        void ReAssignmentVariableILGenerate(ReAssignmentVariableAst reAssignmentAst, GenerateArgs generateArgs)
        {
            var captureIdx = generateArgs.variables.GetCaptureVariableIdx(reAssignmentAst.variableAst.VarName);
            if (captureIdx.HasValue)
            {
                var ast =
                    InstanceFuncCallAst.Create(
                        VariableAst.Create(new Token(default, "_action", default)),
                        FuncCallAst.Create(
                            new Token(default, "set", default),
                            new[] { ValueAst.Create(captureIdx.Value, default), reAssignmentAst.expr },
                            new Token(default, "")
                        )
                    );
                ExprILGenerate(ast, generateArgs);
            }
            else
            {
                ExprILGenerate(reAssignmentAst.expr, generateArgs);
                marineILs.Add(new StoreIL(generateArgs.variables.GetVariableIdx(reAssignmentAst.variableAst.VarName)));
            }
        }

        void ReAssignmentIndexerILGenerate(ReAssignmentIndexerAst reAssignmentAst, GenerateArgs generateArgs)
        {
            ExprILGenerate(reAssignmentAst.getIndexerAst.instanceExpr, generateArgs);
            ExprILGenerate(reAssignmentAst.getIndexerAst.indexExpr, generateArgs);
            ExprILGenerate(reAssignmentAst.assignmentExpr, generateArgs);
            marineILs.Add(
                new InstanceCSharpIndexerStoreIL(
                    new ILDebugInfo(reAssignmentAst.getIndexerAst.instanceExpr.Range.End)));
        }

        void AssignmentILGenerate(AssignmentVariableAst assignmentAst, GenerateArgs generateArgs)
        {
            ExprILGenerate(assignmentAst.expr, generateArgs);
            generateArgs.variables.AddLocalVariable(assignmentAst.variableAst.VarName);
            marineILs.Add(new StoreIL(generateArgs.variables.GetVariableIdx(assignmentAst.variableAst.VarName)));
        }

        void InstanceFieldAssignmentILGenerate(InstanceFieldAssignmentAst fieldAssignmentAst, GenerateArgs generateArgs)
        {

            ExprILGenerate(fieldAssignmentAst.instanceFieldAst.instanceExpr, generateArgs);
            ExprILGenerate(fieldAssignmentAst.expr, generateArgs);
            marineILs.Add(
                new InstanceCSharpFieldStoreIL(
                    fieldAssignmentAst.instanceFieldAst.variableAst.VarName,
                    new ILDebugInfo(fieldAssignmentAst.instanceFieldAst.variableAst.Range.Start)
                )
            );
        }

        void StaticFieldAssignmentILGenerate(StaticFieldAssignmentAst staticFieldAssignmentAst, GenerateArgs generateArgs)
        {
            ExprILGenerate(staticFieldAssignmentAst.expr, generateArgs);
            marineILs.Add(
                new StaticCSharpFieldStoreIL(staticTypeDict[staticFieldAssignmentAst.staticFieldAst.ClassName],
                    staticFieldAssignmentAst.staticFieldAst.variableAst.VarName,
                    new ILDebugInfo(staticFieldAssignmentAst.Range.Start)
                )
            );
        }

        void WhileILGenerate(WhileAst whileAst, GenerateArgs generateArgs)
        {
            generateArgs.variables.InScope();
            generateArgs.breakIndex = new BreakIndex();

            var jumpIL = new JumpIL(marineILs.Count);
            ExprILGenerate(whileAst.conditionExpr, generateArgs);
            var jumpFalseILInsertIndex = marineILs.Count;
            marineILs.Add(null);
            foreach (var statementAst in whileAst.statements)
                StatementILGenerate(statementAst, generateArgs);
            marineILs.Add(jumpIL);
            marineILs[jumpFalseILInsertIndex] = new JumpFalseIL(marineILs.Count);
            generateArgs.breakIndex.Index = marineILs.Count;
            generateArgs.variables.OutScope();
        }

        void ForILGenerate(ForAst forAst, GenerateArgs generateArgs)
        {
            generateArgs.variables.InScope();
            generateArgs.variables.AddLocalVariable(forAst.initVariable.VarName);
            generateArgs.breakIndex = new BreakIndex();
            var countVarIdx = generateArgs.variables.GetVariableIdx(forAst.initVariable.VarName);
            var maxVarIdx = generateArgs.variables.CreateUnnamedLocalVariableIdx();
            var addVarIdx = generateArgs.variables.CreateUnnamedLocalVariableIdx();
            ExprILGenerate(forAst.initExpr, generateArgs);
            marineILs.Add(new StoreIL(countVarIdx));
            ExprILGenerate(forAst.maxValueExpr, generateArgs);
            marineILs.Add(new StoreIL(maxVarIdx));
            ExprILGenerate(forAst.addValueExpr, generateArgs);
            marineILs.Add(new StoreIL(addVarIdx));
            var jumpIL = new JumpIL(marineILs.Count);
            marineILs.Add(new LoadIL(countVarIdx));
            marineILs.Add(new LoadIL(maxVarIdx));
            marineILs.Add(new BinaryOpIL(TokenType.LessEqualOp));
            var jumpFalseILInsertIndex = marineILs.Count;
            marineILs.Add(null);
            foreach (var statementAst in forAst.statements)
                StatementILGenerate(statementAst, generateArgs);
            marineILs.Add(new LoadIL(countVarIdx));
            marineILs.Add(new LoadIL(addVarIdx));
            marineILs.Add(new BinaryOpIL(TokenType.PlusOp));
            marineILs.Add(new StoreIL(countVarIdx));
            marineILs.Add(jumpIL);
            marineILs[jumpFalseILInsertIndex] = new JumpFalseIL(marineILs.Count);
            generateArgs.breakIndex.Index = marineILs.Count;
            generateArgs.variables.OutScope();
        }

        void ForEachILGenerate(ForEachAst forEachAst, GenerateArgs generateArgs)
        {
            generateArgs.variables.InScope();
            generateArgs.variables.AddLocalVariable(forEachAst.variable.VarName);

            generateArgs.breakIndex = new BreakIndex();
            var currentVarIdx = generateArgs.variables.GetVariableIdx(forEachAst.variable.VarName);
            var iterVarIdx = generateArgs.variables.CreateUnnamedLocalVariableIdx();
            ExprILGenerate(forEachAst.expr, generateArgs);
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
                StatementILGenerate(statementAst, generateArgs);
            marineILs.Add(jumpIL);
            marineILs[jumpFalseILInsertIndex] = new JumpFalseIL(marineILs.Count);
            generateArgs.breakIndex.Index = marineILs.Count;
            generateArgs.variables.OutScope();
        }

        void ValueILGenerate(ValueAst valueAst)
        {
            marineILs.Add(new PushValueIL(valueAst.value));
        }
    }
}