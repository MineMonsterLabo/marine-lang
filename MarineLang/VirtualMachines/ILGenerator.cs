using MarineLang.BuiltInTypes;
using MarineLang.Models;
using MarineLang.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MarineLang.VirtualMachines
{
    using VariableDict = Dictionary<string, int>;

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
            VariableDict variables = Enumerable.Range(0, funcDefinitionAst.args.Length)
                .ToDictionary(idx => funcDefinitionAst.args[idx].varName, idx => idx + 1);
            var baseVarIdx = variables.Count + VirtualMachineConstants.CALL_RESTORE_STACK_FRAME + 1;
            var varIdx = 0;
            foreach (var varName in GetAssignmentAsts(funcDefinitionAst).Select(x => x.varName))
            {
                variables.Add(varName, varIdx + baseVarIdx);
                varIdx++;
            }
            if (varIdx != 0)
                marineILs.Add(new StackAllocIL(varIdx));
            foreach (var statementAst in funcDefinitionAst.statementAsts)
                retFlag = retFlag || StatementILGenerate(statementAst, funcDefinitionAst.args.Length, variables);
            if (funcDefinitionAst.statementAsts.Length == 0)
                marineILs.Add(new NoOpIL());
            if (retFlag == false)
            {
                marineILs.Add(new PushValueIL(new UnitType()));
                marineILs.Add(new RetIL(funcDefinitionAst.args.Length));
            }
        }

        bool StatementILGenerate(StatementAst statementAst, int argCount, VariableDict variables)
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

        void ReturnILGenerate(ReturnAst returnAst, int argCount, VariableDict variables)
        {
            ExprILGenerate(returnAst.expr, argCount, variables);
            marineILs.Add(new RetIL(argCount));
        }

        void ExprILGenerate(ExprAst exprAst, int argCount, VariableDict variables)
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

        void FuncCallILGenerate(FuncCallAst funcCallAst, int argCount, VariableDict args)
        {
            foreach (var exprAst in funcCallAst.args)
                ExprILGenerate(exprAst, argCount, args);

            if (funcILIndexDict.ContainsKey(funcCallAst.funcName))
                marineILs.Add(new MarineFuncCallIL(funcCallAst.funcName, funcCallAst.args.Length));
            else
                marineILs.Add(new CSharpFuncCallIL(csharpFuncDict[funcCallAst.funcName], funcCallAst.args.Length));
        }

        void BinaryOpILGenerate(BinaryOpAst binaryOpAst, int argCount, VariableDict args)
        {
            ExprILGenerate(binaryOpAst.leftExpr, argCount, args);
            ExprILGenerate(binaryOpAst.rightExpr, argCount, args);
            marineILs.Add(new BinaryOpIL(binaryOpAst.opKind));
        }

        void VariableILGenerate(VariableAst variableAst, VariableDict variables)
        {
            marineILs.Add(new LoadIL(variables[variableAst.varName]));
        }

        void IfILGenerate(IfExprAst ifExprAst, int argCount, VariableDict variables)
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

        void InstanceFuncCallILGenerate(InstanceFuncCallAst instanceFuncCallAst, int argCount, VariableDict variables)
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

        void InstanceFieldILGenerate(InstanceFieldAst instanceFieldAst, int argCount, VariableDict variables)
        {
            ExprILGenerate(instanceFieldAst.instanceExpr, argCount, variables);
            marineILs.Add(
                new InstanceCSharpFieldLoadIL(instanceFieldAst.fieldName)
            );
        }

        void ReAssignmentILGenerate(ReAssignmentAst reAssignmentAst, int argCount, VariableDict variables)
        {
            ExprILGenerate(reAssignmentAst.expr, argCount, variables);
            marineILs.Add(new StoreIL(variables[reAssignmentAst.varName]));
        }

        void AssignmentILGenerate(AssignmentAst assignmentAst, int argCount, VariableDict variables)
        {
            ExprILGenerate(assignmentAst.expr, argCount, variables);
            marineILs.Add(new StoreIL(variables[assignmentAst.varName]));
        }

        void FieldAssignmentILGenerate(FieldAssignmentAst fieldAssignmentAst, int argCount, VariableDict variables)
        {
            ExprILGenerate(fieldAssignmentAst.instanceExpr, argCount, variables);
            ExprILGenerate(fieldAssignmentAst.expr, argCount, variables);
            marineILs.Add(new InstanceCSharpFieldStoreIL(fieldAssignmentAst.fieldName));
        }

        void WhileILGenerate(WhileAst whileAst, int argCount, VariableDict variables)
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

        IEnumerable<AssignmentAst> GetAssignmentAsts(FuncDefinitionAst funcDefinitionAst)
        {
            return funcDefinitionAst.statementAsts.SelectMany(GetAssignmentAsts);
        }

        IEnumerable<AssignmentAst> GetAssignmentAsts(StatementAst statementAst)
        {
            if (statementAst.GetExprAst() != null)
                return GetAssignmentAsts(statementAst.GetExprAst());
            else if (statementAst.GetAssignmentAst() != null)
                return GetAssignmentAsts(statementAst.GetAssignmentAst());
            else if (statementAst.GetReAssignmentAst() != null)
                return GetAssignmentAsts(statementAst.GetReAssignmentAst());
            else if (statementAst.GetReturnAst() != null)
                return GetAssignmentAsts(statementAst.GetReturnAst());

            return Enumerable.Empty<AssignmentAst>();

        }

        IEnumerable<AssignmentAst> GetAssignmentAsts(AssignmentAst assignmentAst)
        {
            return GetAssignmentAsts(assignmentAst.expr).Concat(new[] { assignmentAst });
        }

        IEnumerable<AssignmentAst> GetAssignmentAsts(ReAssignmentAst reAssignmentAst)
        {
            return GetAssignmentAsts(reAssignmentAst.expr);
        }

        IEnumerable<AssignmentAst> GetAssignmentAsts(ReturnAst returnAst)
        {
            return GetAssignmentAsts(returnAst.expr);
        }

        IEnumerable<AssignmentAst> GetAssignmentAsts(ExprAst exprAst)
        {
            if (exprAst.GetFuncCallAst() != null)
                return GetAssignmentAsts(exprAst.GetFuncCallAst());
            else if (exprAst.GetBinaryOpAst() != null)
                return GetAssignmentAsts(exprAst.GetBinaryOpAst());
            else if (exprAst.GetIfExprAst() != null)
                return GetAssignmentAsts(exprAst.GetIfExprAst());
            return Enumerable.Empty<AssignmentAst>();
        }

        IEnumerable<AssignmentAst> GetAssignmentAsts(FuncCallAst funcCallAst)
        {
            return funcCallAst.args.SelectMany(GetAssignmentAsts);
        }

        IEnumerable<AssignmentAst> GetAssignmentAsts(BinaryOpAst binaryOpAst)
        {
            return GetAssignmentAsts(binaryOpAst.leftExpr).Concat(GetAssignmentAsts(binaryOpAst.rightExpr));
        }

        IEnumerable<AssignmentAst> GetAssignmentAsts(IfExprAst ifExprAst)
        {
            return ifExprAst.thenStatements.SelectMany(GetAssignmentAsts)
                .Concat(ifExprAst.elseStatements.SelectMany(GetAssignmentAsts));
        }

    }
}
