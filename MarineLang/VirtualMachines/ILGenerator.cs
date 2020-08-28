using MarineLang.BuiltInTypes;
using MarineLang.Models;
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

    public class ILGenerator
    {
        Dictionary<string, int> funcILIndexDict = new Dictionary<string, int>();
        IReadOnlyDictionary<string, MethodInfo> csharpFuncDict;
        readonly List<IMarineIL> marineILs = new List<IMarineIL>();
        readonly ProgramAst programAst;

        public ILGenerator(ProgramAst programAst)
        {
            this.programAst = programAst;
        }

        public ILGeneratedData Generate(IReadOnlyDictionary<string, MethodInfo> csharpFuncDict)
        {
            this.csharpFuncDict = csharpFuncDict;
            funcILIndexDict = programAst.funcDefinitionAsts.ToDictionary(x => x.funcName, x => -1);
            ProgramILGenerate(programAst);
            return new ILGeneratedData(funcILIndexDict, marineILs);
        }

        void ProgramILGenerate(ProgramAst programAst)
        {
            foreach (var funcDefinitionAst in programAst.funcDefinitionAsts)
                FuncDefinitionILGenerate(funcDefinitionAst);
        }

        void FuncDefinitionILGenerate(FuncDefinitionAst funcDefinitionAst)
        {
            funcILIndexDict[funcDefinitionAst.funcName] = marineILs.Count;
            bool retFlag = false;
            var variables = new FuncScopeVariables(funcDefinitionAst.args);
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

        bool StatementILGenerate(StatementAst statementAst, int argCount, FuncScopeVariables variables)
        {
            if (statementAst.GetReturnAst() != null)
            {
                ReturnILGenerate(statementAst.GetReturnAst(), argCount, variables);
                return true;
            }
            else if (statementAst.GetExprAst() != null)
                ExprILGenerate(statementAst.GetExprAst(), argCount, variables);
            else if (statementAst.GetReAssignmentAst() != null)
                ReAssignmentILGenerate(statementAst.GetReAssignmentAst(), argCount, variables);
            else if (statementAst.GetAssignmentAst() != null)
                AssignmentILGenerate(statementAst.GetAssignmentAst(), argCount, variables);
            else if (statementAst.GetFieldAssignmentAst() != null)
                FieldAssignmentILGenerate(statementAst.GetFieldAssignmentAst(), argCount, variables);
            else if (statementAst.GetWhileAst() != null)
                WhileILGenerate(statementAst.GetWhileAst(), argCount, variables);
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
                VariableILGenerate(exprAst.GetVariableAst(), variables);
            else if (exprAst.GetValueAst() != null)
                ValueILGenerate(exprAst.GetValueAst());
            else if (exprAst.GetIfExprAst() != null)
                IfILGenerate(exprAst.GetIfExprAst(), argCount, variables);
            else if (exprAst.GetInstanceFuncCallAst() != null)
                InstanceFuncCallILGenerate(exprAst.GetInstanceFuncCallAst(), argCount, variables);
            else if (exprAst.GetInstanceFieldAst() != null)
                InstanceFieldILGenerate(exprAst.GetInstanceFieldAst(), argCount, variables);
        }

        void FuncCallILGenerate(FuncCallAst funcCallAst, int argCount, FuncScopeVariables args)
        {
            foreach (var exprAst in funcCallAst.args)
                ExprILGenerate(exprAst, argCount, args);

            if (funcILIndexDict.ContainsKey(funcCallAst.funcName))
                marineILs.Add(new MarineFuncCallIL(funcCallAst.funcName, funcCallAst.args.Length));
            else
                marineILs.Add(new CSharpFuncCallIL(csharpFuncDict[funcCallAst.funcName], funcCallAst.args.Length));
        }

        void BinaryOpILGenerate(BinaryOpAst binaryOpAst, int argCount, FuncScopeVariables args)
        {
            ExprILGenerate(binaryOpAst.leftExpr, argCount, args);
            ExprILGenerate(binaryOpAst.rightExpr, argCount, args);
            marineILs.Add(new BinaryOpIL(binaryOpAst.opKind));
        }

        void VariableILGenerate(VariableAst variableAst, FuncScopeVariables variables)
        {
            marineILs.Add(new LoadIL(variables.GetLocalVariableIdx(variableAst.varName)));
        }

        void IfILGenerate(IfExprAst ifExprAst, int argCount, FuncScopeVariables variables)
        {
            ExprILGenerate(ifExprAst.conditionExpr, argCount, variables);
            var jumpFalseInsertIndex = marineILs.Count;
            marineILs.Add(null);
            foreach (var statementAst in ifExprAst.thenStatements)
                StatementILGenerate(statementAst, argCount, variables);
            var jumpInsertIndex = marineILs.Count;
            marineILs.Add(null);
            marineILs[jumpFalseInsertIndex] = new JumpFalseIL(marineILs.Count);
            if (ifExprAst.elseStatements.Length == 0)
                marineILs.Add(new PushValueIL(new UnitType()));
            else
                foreach (var statementAst in ifExprAst.elseStatements)
                    StatementILGenerate(statementAst, argCount, variables);
            marineILs[jumpInsertIndex] = new JumpIL(marineILs.Count);
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

        void ReAssignmentILGenerate(ReAssignmentAst reAssignmentAst, int argCount, FuncScopeVariables variables)
        {
            ExprILGenerate(reAssignmentAst.expr, argCount, variables);
            marineILs.Add(new StoreIL(variables.GetLocalVariableIdx(reAssignmentAst.varName)));
        }

        void AssignmentILGenerate(AssignmentAst assignmentAst, int argCount, FuncScopeVariables variables)
        {
            ExprILGenerate(assignmentAst.expr, argCount, variables);
            variables.AddLocalVariable(assignmentAst.varName);
            marineILs.Add(new StoreIL(variables.GetLocalVariableIdx(assignmentAst.varName)));
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

        void ValueILGenerate(ValueAst valueAst)
        {
            marineILs.Add(new PushValueIL(valueAst.value));
        }
    }
}
