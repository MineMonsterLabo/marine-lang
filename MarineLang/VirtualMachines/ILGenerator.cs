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
            public string CurrentFuncName { get; }
            public MarineProgramUnit CurrentProgramUnit { get; }
            public int argCount;
            public FuncScopeVariables variables;
            public BreakIndex breakIndex;
            public NamespaceTable GlobalNamespaceTable { get; }
            public NamespaceTable CurrentNamespaceTable { get; }
            public GenerateArgs(
                MarineProgramUnit currentProgramUnit,
                string currentFuncName,
                int argCount,
                FuncScopeVariables variables,
                BreakIndex breakIndex,
                NamespaceTable globalNamespaceTable,
                NamespaceTable currentNamespaceTable
            )
            {
                CurrentFuncName = currentFuncName;
                CurrentProgramUnit = currentProgramUnit;
                this.argCount = argCount;
                this.variables = variables;
                this.breakIndex = breakIndex;
                GlobalNamespaceTable = globalNamespaceTable;
                CurrentNamespaceTable = currentNamespaceTable;
            }
        }

        IReadonlyCsharpFuncTable csharpFuncTable;
        IReadOnlyDictionary<string, Type> staticTypeDict;
        readonly List<IMarineIL> marineILs = new List<IMarineIL>();
        readonly List<ActionFuncData> actionFuncDataList = new List<ActionFuncData>();
        readonly IEnumerable<MarineProgramUnit> marineProgramUnits;

        public ILGenerator(IEnumerable<MarineProgramUnit> marineProgramUnits)
        {
            this.marineProgramUnits = marineProgramUnits;
        }

        public ILGeneratedData Generate(
            IReadonlyCsharpFuncTable csharpFuncTable,
            IReadOnlyDictionary<string, Type> staticTypeDict,
            string[] globalVariableNames
            )
        {
            this.csharpFuncTable = csharpFuncTable;
            this.staticTypeDict = staticTypeDict;
            var namespaceTable = new NamespaceTable();

            foreach (var marineProgramUnit in marineProgramUnits)
            {
                var funcNames = marineProgramUnit.ProgramAst.funcDefinitionAsts.Select(x => x.funcName);
                namespaceTable.AddFuncILIndex(marineProgramUnit.NamespaceStrings, funcNames);
            }

            foreach (var marineProgramUnit in marineProgramUnits)
            {
                ProgramILGenerate(
                    marineProgramUnit,
                    namespaceTable,
                    namespaceTable.GetChildNamespace(marineProgramUnit.NamespaceStrings),
                    marineProgramUnit.ProgramAst,
                    globalVariableNames
                );
            }
            return new ILGeneratedData(namespaceTable, marineILs);
        }

        void ProgramILGenerate(
            MarineProgramUnit currentProgramUnit,
            NamespaceTable globalNamespaceTable,
            NamespaceTable namespaceTable,
            ProgramAst programAst,
            string[] globalVariableNames
        )
        {
            foreach (var funcDefinitionAst in programAst.funcDefinitionAsts)
                FuncDefinitionILGenerate(
                    currentProgramUnit,
                    globalNamespaceTable,
                    namespaceTable,
                    funcDefinitionAst,
                    new FuncScopeVariables(funcDefinitionAst.args, globalVariableNames)
                );

            for (var i = 0; i < actionFuncDataList.Count; i++)
                ActionFuncILGenerate(currentProgramUnit, globalNamespaceTable, namespaceTable, actionFuncDataList[i], globalVariableNames);

            actionFuncDataList.Clear();
        }

        void FuncDefinitionILGenerate(
            MarineProgramUnit currentProgramUnit,
            NamespaceTable globalNamespaceTable,
            NamespaceTable namespaceTable,
            FuncDefinitionAst funcDefinitionAst,
            FuncScopeVariables variables,
            bool isGlobalFunc = false
        )
        {
            if (isGlobalFunc)
            {
                globalNamespaceTable.SetFuncILIndex(funcDefinitionAst.funcName, marineILs.Count);
            }
            else
            {
                namespaceTable.SetFuncILIndex(funcDefinitionAst.funcName, marineILs.Count);
            }

            bool retFlag = false;
            var stackAllockIndex = marineILs.Count;
            marineILs.Add(new NoOpIL());

            foreach (var statementAst in funcDefinitionAst.statementAsts)
                retFlag =
                    retFlag ||
                    StatementILGenerate(
                        statementAst,
                        new GenerateArgs(
                            currentProgramUnit,
                            funcDefinitionAst.funcName,
                            funcDefinitionAst.args.Length, 
                            variables, 
                            breakIndex: null, 
                            globalNamespaceTable, 
                            namespaceTable
                        )
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

        void ActionFuncILGenerate(
            MarineProgramUnit currentProgramUnit,
            NamespaceTable globalNamespaceTable,
            NamespaceTable namespaceTable,
            ActionFuncData actionFuncData,
            string[] globalVariableNames)
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
            FuncDefinitionILGenerate(currentProgramUnit, globalNamespaceTable, namespaceTable, funcDefinitionAst, variables, true);
        }

        bool StatementILGenerate(StatementAst statementAst, GenerateArgs generateArgs)
        {
            marineILs.Add(
                new PushDebugContextIL(
                    new DebugContext(
                        generateArgs.CurrentProgramUnit.Name,
                        generateArgs.CurrentFuncName,
                        statementAst.Range
                    )
                )
            );

            switch (statementAst)
            {
                case ReturnAst returnAst:
                    ReturnILGenerate(returnAst, generateArgs);
                    marineILs.Add(new PopDebugContextIL());
                    return true;
                case ExprStatementAst exprStatementAst:
                    ExprILGenerate(exprStatementAst.expr, generateArgs);
                    marineILs.Add(new PopIL()); break;
                case ReAssignmentVariableAst reAssignmentVariableAst:
                    ReAssignmentVariableILGenerate(reAssignmentVariableAst, generateArgs); break;
                case ReAssignmentIndexerAst reAssignmentIndexerAst:
                    ReAssignmentIndexerILGenerate(reAssignmentIndexerAst, generateArgs); break;
                case AssignmentVariableAst assignmentVariableAst:
                    AssignmentILGenerate(assignmentVariableAst, generateArgs); break;
                case InstanceFieldAssignmentAst instanceFieldAssignmentAst:
                    InstanceFieldAssignmentILGenerate(instanceFieldAssignmentAst, generateArgs); break;
                case StaticFieldAssignmentAst staticFieldAssignmentAst:
                    StaticFieldAssignmentILGenerate(staticFieldAssignmentAst, generateArgs); break;
                case WhileAst whileAst:
                    WhileILGenerate(whileAst, generateArgs); break;
                case ForAst forAst:
                    ForILGenerate(forAst, generateArgs); break;
                case ForEachAst forEachAst:
                    ForEachILGenerate(forEachAst, generateArgs); break;
                case YieldAst yieldAst:
                    YieldILGenerate(yieldAst, generateArgs); break;
                case BreakAst breakAst:
                    marineILs.Add(new BreakIL(generateArgs.breakIndex)); break;
            }

            marineILs.Add(new PopDebugContextIL());
            return false;
        }

        void ReturnILGenerate(ReturnAst returnAst, GenerateArgs generateArgs)
        {
            ExprILGenerate(returnAst.expr, generateArgs);
            marineILs.Add(new RetIL(generateArgs.argCount));
        }

        void YieldILGenerate(YieldAst yieldAst, GenerateArgs generateArgs)
        {
            ExprILGenerate(yieldAst.exprAst, generateArgs);
            marineILs.Add(new YieldIL());
        }

        void ExprILGenerate(ExprAst exprAst, GenerateArgs generateArgs)
        {
            switch (exprAst)
            {
                case FuncCallAst funcCallAst:
                    FuncCallILGenerate(funcCallAst, generateArgs); break;
                case BinaryOpAst binaryOpAst:
                    BinaryOpILGenerate(binaryOpAst, generateArgs); break;
                case VariableAst variableAst:
                    VariableILGenerate(variableAst, generateArgs); break;
                case ValueAst valueAst:
                    ValueILGenerate(valueAst); break;
                case IfExprAst ifExprAst:
                    IfILGenerate(ifExprAst, generateArgs); break;
                case InstanceFuncCallAst instanceFuncCallAst:
                    InstanceFuncCallILGenerate(instanceFuncCallAst, generateArgs); break;
                case StaticFuncCallAst staticFuncCall:
                    StaticFuncCallILGenerate(staticFuncCall, generateArgs); break;
                case InstanceFieldAst instanceFieldAst:
                    InstanceFieldILGenerate(instanceFieldAst, generateArgs); break;
                case StaticFieldAst staticFieldAst:
                    StaticFieldILGenerate(staticFieldAst); break;
                case GetIndexerAst getIndexerAst:
                    GetGetIndexerILGenerate(getIndexerAst, generateArgs); break;
                case ArrayLiteralAst arrayLiteralAst:
                    ArrayLiteralILGenerate(arrayLiteralAst, generateArgs); break;
                case ActionAst actionAst:
                    ActionILGenerate(actionAst, generateArgs); break;
                case AwaitAst awaitAst:
                    AwaitILGenerate(awaitAst, generateArgs); break;
                case UnaryOpAst unaryOpAst:
                    UnaryOpILGenerate(unaryOpAst, generateArgs); break;
                case DictionaryConstructAst dictionaryConstructAst:
                    DictionaryConstructILGenerate(dictionaryConstructAst, generateArgs); break;
                default:
                    throw new Exception("IL生成に失敗");
            };
        }

        void FuncCallILGenerate(FuncCallAst funcCallAst, GenerateArgs generateArgs)
        {
            foreach (var exprAst in funcCallAst.args)
                ExprILGenerate(exprAst, generateArgs);

            var namespaceSettings = funcCallAst.NamespaceSettings;
            var funcName = funcCallAst.FuncName;
            var currentNamespaceTable = generateArgs.CurrentNamespaceTable;
            var globalNamespaceTable = generateArgs.GlobalNamespaceTable;

            if (funcCallAst.namespaceTokens.Any())
            {
                if (globalNamespaceTable.TryGetFuncILIndex(namespaceSettings, funcName, out var funcILIndex))
                {
                    marineILs.Add(
                        new MarineFuncCallIL(
                            funcName,
                            funcILIndex,
                            funcCallAst.args.Length
                        )
                    );
                    return;
                }

                marineILs.Add(
                   new CSharpFuncCallIL(
                       csharpFuncTable.GetCsharpFunc(namespaceSettings,NameUtil.GetUpperCamelName(funcName)),
                       funcCallAst.args.Length
                   )
               );
                return;
            }

            if (currentNamespaceTable.ContainFunc(funcName))
                marineILs.Add(
                    new MarineFuncCallIL(
                        funcName,
                        currentNamespaceTable.GetFuncILIndex(funcName),
                        funcCallAst.args.Length
                    )
                );
            else if (globalNamespaceTable.ContainFunc(funcName))
                marineILs.Add(
                    new MarineFuncCallIL(
                        funcName,
                        globalNamespaceTable.GetFuncILIndex(funcName),
                        funcCallAst.args.Length
                    )
                );
            else
                marineILs.Add(
                    new CSharpFuncCallIL(
                        csharpFuncTable.GetCsharpFunc(NameUtil.GetUpperCamelName(funcName)),
                        funcCallAst.args.Length
                    )
                );
        }

        void BinaryOpILGenerate(BinaryOpAst binaryOpAst, GenerateArgs generateArgs)
        {
            if (binaryOpAst.opKind == TokenType.OrOp)
            {
                ExprILGenerate(binaryOpAst.leftExpr, generateArgs);
                var jumpInsertIndex = marineILs.Count;
                marineILs.Add(null);
                ExprILGenerate(binaryOpAst.rightExpr, generateArgs);
                marineILs[jumpInsertIndex] = new JumpTrueNoPopIL(marineILs.Count);
            }
            else if (binaryOpAst.opKind == TokenType.AndOp)
            {
                ExprILGenerate(binaryOpAst.leftExpr, generateArgs);
                var jumpInsertIndex = marineILs.Count;
                marineILs.Add(null);
                ExprILGenerate(binaryOpAst.rightExpr, generateArgs);
                marineILs[jumpInsertIndex] = new JumpFalseNoPopIL(marineILs.Count);
            }
            else
            {
                ExprILGenerate(binaryOpAst.leftExpr, generateArgs);
                ExprILGenerate(binaryOpAst.rightExpr, generateArgs);
                marineILs.Add(new BinaryOpIL(binaryOpAst.opKind));
            }
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
                            new string[] { },
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
                new InstanceCSharpFuncCallIL(csharpFuncName, funcCallAst.args.Length, GetGenericTypes(funcCallAst.genericTypeNames))
            );
        }

        void StaticFuncCallILGenerate(StaticFuncCallAst staticFuncCallAst, GenerateArgs generateArgs)
        {

            var funcCallAst = staticFuncCallAst.funcCallAst;
            var csharpFuncName = NameUtil.GetUpperCamelName(funcCallAst.FuncName);
            foreach (var arg in funcCallAst.args)
                ExprILGenerate(arg, generateArgs);

            var type = staticTypeDict[staticFuncCallAst.ClassName];

            if(csharpFuncName == "New")
            {
                marineILs.Add(
                    new StaticCSharpConstructorCallIL(
                        type,
                        type.GetConstructors(), 
                        csharpFuncName,
                        funcCallAst.args.Length
                    )
                );
            }
            else
            {
                var methodInfos=
                    type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .Where(e => e.Name == csharpFuncName)
                    .ToArray();

                marineILs.Add(
                    new StaticCSharpFuncCallIL(
                        type,
                        methodInfos,
                        csharpFuncName,
                        funcCallAst.args.Length,
                        GetGenericTypes(funcCallAst.genericTypeNames)
                    )
                );
            }
        }

        void InstanceFieldILGenerate(InstanceFieldAst instanceFieldAst, GenerateArgs generateArgs)
        {
            ExprILGenerate(instanceFieldAst.instanceExpr, generateArgs);
            marineILs.Add(
                new InstanceCSharpFieldLoadIL(
                    instanceFieldAst.variableAst.VarName
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
                        staticFieldAst.variableAst.VarName
                    )
                );
            }
        }

        void GetGetIndexerILGenerate(GetIndexerAst getIndexerAst, GenerateArgs generateArgs)
        {
            ExprILGenerate(getIndexerAst.instanceExpr, generateArgs);
            ExprILGenerate(getIndexerAst.indexExpr, generateArgs);
            marineILs.Add(
                new InstanceCSharpIndexerLoadIL()
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
                        new string[] { },
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
            marineILs.Add(new StoreIL(iterVariable));
            var jumpIndex = marineILs.Count;
            marineILs.Add(new LoadIL(iterVariable));
            marineILs.Add(new MoveNextIL());
            marineILs.Add(new JumpFalseIL(jumpIndex + 7));
            marineILs.Add(new LoadIL(iterVariable));
            marineILs.Add(new GetIterCurrentIL());
            marineILs.Add(new YieldIL());
            marineILs.Add(new JumpIL(jumpIndex));
            marineILs.Add(new PushYieldCurrentRegisterIL());
        }

        void UnaryOpILGenerate(UnaryOpAst unaryOpAst, GenerateArgs generateArgs)
        {
            ExprILGenerate(unaryOpAst.expr, generateArgs);
            marineILs.Add(new UnaryOpIL(unaryOpAst.opToken.tokenType));
        }

        void DictionaryConstructILGenerate(DictionaryConstructAst dictionaryConstructAst, GenerateArgs generateArgs)
        {
            var type = typeof(Dictionary<string, object>);
            var dictVarIdx = generateArgs.variables.CreateUnnamedLocalVariableIdx();

            marineILs.Add(
                new StaticCSharpConstructorCallIL(type, type.GetConstructors(), "New", 0)
            );
            marineILs.Add(new StoreIL(dictVarIdx));
            foreach (var keyValuePair in dictionaryConstructAst.dict)
            {
                marineILs.Add(new LoadIL(dictVarIdx));
                marineILs.Add(new PushValueIL(keyValuePair.Key));
                ExprILGenerate(keyValuePair.Value, generateArgs);
                marineILs.Add(new InstanceCSharpFuncCallIL("Add", 2));
            }
            marineILs.Add(new LoadIL(dictVarIdx));
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
                            new string[] { },
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
                new InstanceCSharpIndexerStoreIL());
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
                    fieldAssignmentAst.instanceFieldAst.variableAst.VarName
                )
            );
        }

        void StaticFieldAssignmentILGenerate(StaticFieldAssignmentAst staticFieldAssignmentAst, GenerateArgs generateArgs)
        {
            ExprILGenerate(staticFieldAssignmentAst.expr, generateArgs);
            marineILs.Add(
                new StaticCSharpFieldStoreIL(staticTypeDict[staticFieldAssignmentAst.staticFieldAst.ClassName],
                    staticFieldAssignmentAst.staticFieldAst.variableAst.VarName
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
            marineILs.Add(new InstanceCSharpFieldLoadIL("Current"));
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

        Type[] GetGenericTypes(string[] genericTypeNames)
        {
            return genericTypeNames?.Select(Type.GetType).ToArray();
        }
    }
}