﻿using MarineLang.BuiltInTypes;
using MarineLang.Models;
using MarineLang.Models.Asts;
using MarineLang.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MarineLang.VirtualMachines
{
    public class ILGeneratedData
    {
        public readonly IReadOnlyDictionary<string, int> funcILIndexDict;
        public readonly IReadOnlyList<IMarineIL> marineILs;

        public ILGeneratedData(IReadOnlyDictionary<string, int> funcILIndexDict, IReadOnlyList<IMarineIL> marineILs)
        {
            this.funcILIndexDict = funcILIndexDict;
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

    public class ILGenerator
    {
        Dictionary<string, int> funcILIndexDict = new Dictionary<string, int>();
        IReadOnlyDictionary<string, MethodInfo> csharpFuncDict;
        readonly List<IMarineIL> marineILs = new List<IMarineIL>();
        readonly List<ActionFuncData> actionFuncDataList = new List<ActionFuncData>();
        readonly ProgramAst programAst;

        public ILGenerator(ProgramAst programAst)
        {
            this.programAst = programAst;
        }

        public ILGeneratedData Generate(IReadOnlyDictionary<string, MethodInfo> csharpFuncDict, string[] globalVariableNames)
        {
            this.csharpFuncDict = csharpFuncDict;
            funcILIndexDict = programAst.funcDefinitionAsts.ToDictionary(x => x.funcName, x => -1);
            ProgramILGenerate(programAst, globalVariableNames);
            return new ILGeneratedData(funcILIndexDict, marineILs);
        }

        void ProgramILGenerate(ProgramAst programAst, string[] globalVariableNames)
        {
            foreach (var funcDefinitionAst in programAst.funcDefinitionAsts)
                FuncDefinitionILGenerate(funcDefinitionAst, new FuncScopeVariables(funcDefinitionAst.args, globalVariableNames));
            for (var i = 0; i < actionFuncDataList.Count; i++)
                ActionFuncILGenerate(actionFuncDataList[i], globalVariableNames);
        }

        void FuncDefinitionILGenerate(FuncDefinitionAst funcDefinitionAst, FuncScopeVariables variables)
        {
            funcILIndexDict[funcDefinitionAst.funcName] = marineILs.Count;
            bool retFlag = false;
            var stackAllockIndex = marineILs.Count;
            marineILs.Add(new NoOpIL());
            foreach (var statementAst in funcDefinitionAst.statementAsts)
                retFlag = retFlag || StatementILGenerate(statementAst, funcDefinitionAst.args.Length, variables);
            if (funcDefinitionAst.statementAsts.Length == 0)
                marineILs.Add(new NoOpIL());
            if (retFlag == false)
            {
                marineILs.Add(new PushValueIL(new UnitType()));
                marineILs.Add(new RetIL(funcDefinitionAst.args.Length));
            }

            if (variables.LocalVariableCount != 0)
                marineILs[stackAllockIndex] = new StackAllocIL(variables.LocalVariableCount);
        }

        void ActionFuncILGenerate(ActionFuncData actionFuncData, string[] globalVariableNames)
        {
            var args = new[] { VariableAst.Create("_action") }.Concat(actionFuncData.actionAst.args).ToArray();
            var variables = new FuncScopeVariables(args, globalVariableNames, actionFuncData.captureVarNames);
            var funcDefinitionAst = FuncDefinitionAst.Create(
                actionFuncData.funcName,
                args,
                actionFuncData.actionAst.statementAsts
            );
            FuncDefinitionILGenerate(funcDefinitionAst, variables);
        }

        bool StatementILGenerate(StatementAst statementAst, int argCount, FuncScopeVariables variables)
        {
            if (statementAst.GetReturnAst() != null)
            {
                ReturnILGenerate(statementAst.GetReturnAst(), argCount, variables);
                return true;
            }
            else if (statementAst.GetExprAst() != null)
            {
                ExprILGenerate(statementAst.GetExprAst(), argCount, variables);
                marineILs.Add(new PopIL());
            }
            else if (statementAst.GetReAssignmentVariableAst() != null)
                ReAssignmentVariableILGenerate(statementAst.GetReAssignmentVariableAst(), argCount, variables);
            else if (statementAst.GetReAssignmentIndexerAst() != null)
                ReAssignmentIndexerILGenerate(statementAst.GetReAssignmentIndexerAst(), argCount, variables);
            else if (statementAst.GetAssignmentAst() != null)
                AssignmentILGenerate(statementAst.GetAssignmentAst(), argCount, variables);
            else if (statementAst.GetFieldAssignmentAst() != null)
                FieldAssignmentILGenerate(statementAst.GetFieldAssignmentAst(), argCount, variables);
            else if (statementAst.GetWhileAst() != null)
                WhileILGenerate(statementAst.GetWhileAst(), argCount, variables);
            else if (statementAst.GetForAst() != null)
                ForILGenerate(statementAst.GetForAst(), argCount, variables);
            else if (statementAst.GetYieldAst() != null)
                marineILs.Add(new YieldIL());
            return false;
        }

        void ReturnILGenerate(ReturnAst returnAst, int argCount, FuncScopeVariables variables)
        {
            ExprILGenerate(returnAst.expr, argCount, variables);
            marineILs.Add(new RetIL(argCount));
        }

        void ExprILGenerate(ExprAst exprAst, int argCount, FuncScopeVariables variables)
        {
            if (exprAst.GetFuncCallAst() != null)
                FuncCallILGenerate(exprAst.GetFuncCallAst(), argCount, variables);
            else if (exprAst.GetBinaryOpAst() != null)
                BinaryOpILGenerate(exprAst.GetBinaryOpAst(), argCount, variables);
            else if (exprAst.GetVariableAst() != null)
                VariableILGenerate(exprAst.GetVariableAst(), argCount, variables);
            else if (exprAst.GetValueAst() != null)
                ValueILGenerate(exprAst.GetValueAst());
            else if (exprAst.GetIfExprAst() != null)
                IfILGenerate(exprAst.GetIfExprAst(), argCount, variables);
            else if (exprAst.GetInstanceFuncCallAst() != null)
                InstanceFuncCallILGenerate(exprAst.GetInstanceFuncCallAst(), argCount, variables);
            else if (exprAst.GetInstanceFieldAst() != null)
                InstanceFieldILGenerate(exprAst.GetInstanceFieldAst(), argCount, variables);
            else if (exprAst.GetGetIndexerAst() != null)
                GetGetIndexerILGenerate(exprAst.GetGetIndexerAst(), argCount, variables);
            else if (exprAst.GetArrayLiteralAst() != null)
                ArrayLiteralILGenerate(exprAst.GetArrayLiteralAst(), argCount, variables);
            else if (exprAst.GetActionAst() != null)
                ActionILGenerate(exprAst.GetActionAst(), argCount, variables);
            else if (exprAst.GetAwaitAst() != null)
                AwaitILGenerate(exprAst.GetAwaitAst(), argCount, variables);
            else if (exprAst.GetUnaryOpAst() != null)
                UnaryOpILGenerate(exprAst.GetUnaryOpAst(), argCount, variables);
        }

        void FuncCallILGenerate(FuncCallAst funcCallAst, int argCount, FuncScopeVariables args)
        {
            foreach (var exprAst in funcCallAst.args)
                ExprILGenerate(exprAst, argCount, args);

            if (funcILIndexDict.ContainsKey(funcCallAst.funcName))
                marineILs.Add(new MarineFuncCallIL(funcCallAst.funcName, funcCallAst.args.Length));
            else
                marineILs.Add(new CSharpFuncCallIL(csharpFuncDict[NameUtil.GetUpperCamelName(funcCallAst.funcName)], funcCallAst.args.Length));
        }

        void BinaryOpILGenerate(BinaryOpAst binaryOpAst, int argCount, FuncScopeVariables args)
        {
            ExprILGenerate(binaryOpAst.leftExpr, argCount, args);
            ExprILGenerate(binaryOpAst.rightExpr, argCount, args);
            marineILs.Add(new BinaryOpIL(binaryOpAst.opKind));
        }

        void VariableILGenerate(VariableAst variableAst, int argCount, FuncScopeVariables variables)
        {
            var captureIdx = variables.GetCaptureVariableIdx(variableAst.varName);
            if (captureIdx.HasValue)
            {
                var ast =
                    InstanceFuncCallAst.Create(
                        VariableAst.Create("_action"),
                        FuncCallAst.Create("get", new[] { ValueAst.Create(captureIdx.Value) })
                    );
                ExprILGenerate(ast, argCount, variables);
            }
            else
                marineILs.Add(new LoadIL(variables.GetVariableIdx(variableAst.varName)));
        }

        void IfILGenerate(IfExprAst ifExprAst, int argCount, FuncScopeVariables variables)
        {
            ExprILGenerate(ifExprAst.conditionExpr, argCount, variables);
            var jumpFalseInsertIndex = marineILs.Count;
            marineILs.Add(null);
            BlockExprGenerate(ifExprAst.thenStatements, argCount, variables);
            var jumpInsertIndex = marineILs.Count;
            marineILs.Add(null);
            marineILs[jumpFalseInsertIndex] = new JumpFalseIL(marineILs.Count);
            if (ifExprAst.elseStatements.Length == 0)
                marineILs.Add(new PushValueIL(new UnitType()));
            else
                BlockExprGenerate(ifExprAst.elseStatements, argCount, variables);
            marineILs[jumpInsertIndex] = new JumpIL(marineILs.Count);
        }

        void BlockExprGenerate(StatementAst[] statementAsts, int argCount, FuncScopeVariables variables)
        {
            for (var i = 0; i < statementAsts.Length - 1; i++)
                StatementILGenerate(statementAsts[i], argCount, variables);

            if (statementAsts.Length > 0)
            {
                var lastStatementAst = statementAsts.Last();
                if (lastStatementAst.GetExprAst() != null)
                {
                    ExprILGenerate(lastStatementAst.GetExprAst(), argCount, variables);
                    return;
                }
                StatementILGenerate(lastStatementAst, argCount, variables);
            }

            marineILs.Add(new PushValueIL(new UnitType()));
        }

        void InstanceFuncCallILGenerate(InstanceFuncCallAst instanceFuncCallAst, int argCount, FuncScopeVariables variables)
        {
            ExprILGenerate(instanceFuncCallAst.instanceExpr, argCount, variables);
            var funcCallAst = instanceFuncCallAst.instancefuncCallAst;
            var csharpFuncName = NameUtil.GetUpperCamelName(funcCallAst.funcName);

            foreach (var arg in funcCallAst.args)
                ExprILGenerate(arg, argCount, variables);
            marineILs.Add(
                new InstanceCSharpFuncCallIL(csharpFuncName, funcCallAst.args.Length)
            );
        }

        void InstanceFieldILGenerate(InstanceFieldAst instanceFieldAst, int argCount, FuncScopeVariables variables)
        {
            ExprILGenerate(instanceFieldAst.instanceExpr, argCount, variables);
            marineILs.Add(
                new InstanceCSharpFieldLoadIL(instanceFieldAst.fieldName)
            );
        }

        void GetGetIndexerILGenerate(GetIndexerAst getIndexerAst, int argCount, FuncScopeVariables variables)
        {
            ExprILGenerate(getIndexerAst.instanceExpr, argCount, variables);
            ExprILGenerate(getIndexerAst.indexExpr, argCount, variables);
            marineILs.Add(
                new InstanceCSharpIndexerLoadIL()
            );
        }

        void ArrayLiteralILGenerate(ArrayLiteralAst arrayLiteralAst, int argCount, FuncScopeVariables variables)
        {
            foreach (var exprAst in arrayLiteralAst.exprAsts)
                ExprILGenerate(exprAst, argCount, variables);
            marineILs.Add(new CreateArrayIL(arrayLiteralAst.exprAsts.Length, arrayLiteralAst.size));
        }

        void ActionILGenerate(ActionAst actionAst, int argCount, FuncScopeVariables variables)
        {

            var captures =
                actionAst.statementAsts.SelectMany(x => x.LookUp<VariableAst>())
                .Where(x => variables.ExistVariable(x.varName))
                .Distinct(new VariableAst.Comparer())
                .ToArray();

            var actionFuncName = "_action_func<" + actionFuncDataList.Count + ">";
            actionFuncDataList.Add(new ActionFuncData(actionFuncName, actionAst, captures.Select(x => x.varName).ToArray()));

            var ast =
                InstanceFuncCallAst.Create(
                    VariableAst.Create("action_object_generator"),
                    FuncCallAst.Create(
                        "generate",
                        new ExprAst[] {
                            ValueAst.Create(actionFuncName),
                            ArrayLiteralAst.Create(captures, captures.Length)
                        }
                    )
                );
            ExprILGenerate(ast, argCount, variables);
        }

        void AwaitILGenerate(AwaitAst awaitAst, int argCount, FuncScopeVariables variables)
        {
            ExprILGenerate(awaitAst.instanceExpr, argCount, variables);
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

        void UnaryOpILGenerate(UnaryOpAst unaryOpAst, int argCount, FuncScopeVariables variables)
        {
            ExprILGenerate(unaryOpAst.expr, argCount, variables);
            marineILs.Add(new UnaryOpIL(unaryOpAst.opKind));
        }

        void ReAssignmentVariableILGenerate(ReAssignmentVariableAst reAssignmentAst, int argCount, FuncScopeVariables variables)
        {
            var captureIdx = variables.GetCaptureVariableIdx(reAssignmentAst.varName);
            if (captureIdx.HasValue)
            {
                var ast =
                   InstanceFuncCallAst.Create(
                       VariableAst.Create("_action"),
                       FuncCallAst.Create("set", new[] { ValueAst.Create(captureIdx.Value), reAssignmentAst.expr })
                   );
                ExprILGenerate(ast, argCount, variables);
            }
            else
            {
                ExprILGenerate(reAssignmentAst.expr, argCount, variables);
                marineILs.Add(new StoreIL(variables.GetVariableIdx(reAssignmentAst.varName)));
            }
        }

        void ReAssignmentIndexerILGenerate(ReAssignmentIndexerAst reAssignmentAst, int argCount, FuncScopeVariables variables)
        {
            ExprILGenerate(reAssignmentAst.instanceExpr, argCount, variables);
            ExprILGenerate(reAssignmentAst.indexExpr, argCount, variables);
            ExprILGenerate(reAssignmentAst.assignmentExpr, argCount, variables);
            marineILs.Add(new InstanceCSharpIndexerStoreIL());
        }

        void AssignmentILGenerate(AssignmentVariableAst assignmentAst, int argCount, FuncScopeVariables variables)
        {
            ExprILGenerate(assignmentAst.expr, argCount, variables);
            variables.AddLocalVariable(assignmentAst.varName);
            marineILs.Add(new StoreIL(variables.GetVariableIdx(assignmentAst.varName)));
        }

        void FieldAssignmentILGenerate(FieldAssignmentAst fieldAssignmentAst, int argCount, FuncScopeVariables variables)
        {
            ExprILGenerate(fieldAssignmentAst.instanceExpr, argCount, variables);
            ExprILGenerate(fieldAssignmentAst.expr, argCount, variables);
            marineILs.Add(new InstanceCSharpFieldStoreIL(fieldAssignmentAst.fieldName));
        }

        void WhileILGenerate(WhileAst whileAst, int argCount, FuncScopeVariables variables)
        {
            var jumpIL = new JumpIL(marineILs.Count);
            ExprILGenerate(whileAst.conditionExpr, argCount, variables);
            var jumpFalseILInsertIndex = marineILs.Count;
            marineILs.Add(null);
            foreach (var statementAst in whileAst.statements)
                StatementILGenerate(statementAst, argCount, variables);
            marineILs.Add(jumpIL);
            marineILs[jumpFalseILInsertIndex] = new JumpFalseIL(marineILs.Count);
        }

        void ForILGenerate(ForAst forAst, int argCount, FuncScopeVariables variables)
        {
            variables.AddLocalVariable(forAst.initVariable.varName);
            var countVarIdx = variables.GetVariableIdx(forAst.initVariable.varName);
            var maxVarIdx = variables.CreateUnnamedLocalVariableIdx();
            var addVarIdx = variables.CreateUnnamedLocalVariableIdx();
            ExprILGenerate(forAst.initExpr, argCount, variables);
            marineILs.Add(new StoreIL(countVarIdx));
            ExprILGenerate(forAst.maxValueExpr, argCount, variables);
            marineILs.Add(new StoreIL(maxVarIdx));
            ExprILGenerate(forAst.addValueExpr, argCount, variables);
            marineILs.Add(new StoreIL(addVarIdx));
            var jumpIL = new JumpIL(marineILs.Count);
            marineILs.Add(new LoadIL(countVarIdx));
            marineILs.Add(new LoadIL(maxVarIdx));
            marineILs.Add(new BinaryOpIL(TokenType.LessEqualOp));
            var jumpFalseILInsertIndex = marineILs.Count;
            marineILs.Add(null);
            foreach (var statementAst in forAst.statements)
                StatementILGenerate(statementAst, argCount, variables);
            marineILs.Add(new LoadIL(countVarIdx));
            marineILs.Add(new LoadIL(addVarIdx));
            marineILs.Add(new BinaryOpIL(TokenType.PlusOp));
            marineILs.Add(new StoreIL(countVarIdx));
            marineILs.Add(jumpIL);
            marineILs[jumpFalseILInsertIndex] = new JumpFalseIL(marineILs.Count);
        }

        void ValueILGenerate(ValueAst valueAst)
        {
            marineILs.Add(new PushValueIL(valueAst.value));
        }
    }
}
